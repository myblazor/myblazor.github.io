#!/bin/bash
# =============================================================================
# Observer Magazine — Project Export for LLM Analysis
# =============================================================================
# Exports all git-tracked source files into a single dump.txt for feeding
# into an LLM context window.
#
# Key behaviors:
#   - Starts with a full directory tree of every git-tracked file
#   - Blog posts (content/blog/*.md): front matter ONLY (no body text)
#   - Author files (content/authors/*.yml): full content
#   - All other text files: full content
#   - Skips: binary files, export.sh itself, docs/ folder, dump output
# =============================================================================

set -euo pipefail

OUTPUT_DIR="docs/llm"
OUTPUT_FILE="$OUTPUT_DIR/dump.txt"
mkdir -p "$OUTPUT_DIR"

# Clear/create output file with header
{
    echo "==============================================================================="
    echo "PROJECT EXPORT: $(basename "$(pwd)")"
    echo "DATE: $(date -u '+%Y-%m-%d %H:%M:%S UTC')"
    echo "==============================================================================="
    echo ""
} > "$OUTPUT_FILE"

echo "🚀 Starting export of git-tracked files..."

# ─────────────────────────────────────────────────────────────────────────────
# SECTION 1: Directory tree of ALL git-tracked files
# ─────────────────────────────────────────────────────────────────────────────
{
    echo "==============================================================================="
    echo "DIRECTORY TREE (all git-tracked files)"
    echo "==============================================================================="
    echo ""

    # If 'tree' is available and supports --fromfile, use it for nice formatting.
    # Otherwise fall back to a simple sorted listing with indentation.
    if command -v tree &>/dev/null && tree --version 2>&1 | grep -q 'tree'; then
        git ls-files | tree --fromfile -a --noreport 2>/dev/null || git ls-files | sort
    else
        # Simple fallback: print a clean sorted list with directory prefixes
        git ls-files | sort | while IFS= read -r path; do
            # Calculate depth for indentation
            depth=$(echo "$path" | tr -cd '/' | wc -c)
            indent=""
            for ((i = 0; i < depth; i++)); do
                indent="$indent  "
            done
            basename=$(basename "$path")
            echo "${indent}${basename}"
        done

        # Also print the raw sorted paths for clarity
        echo ""
        echo "--- Raw file paths ---"
        git ls-files | sort
    fi

    echo ""
    echo ""
} >> "$OUTPUT_FILE"

# ─────────────────────────────────────────────────────────────────────────────
# SECTION 2: File contents
# ─────────────────────────────────────────────────────────────────────────────

COUNTER=0
BLOG_COUNTER=0
SKIPPED_COUNTER=0

# extract_front_matter: prints only the YAML front matter from a markdown file.
# Reads from line 1; if it starts with '---', prints lines until the closing '---'.
extract_front_matter() {
    local filepath="$1"
    local in_front_matter=0
    local line_num=0

    while IFS= read -r line; do
        line_num=$((line_num + 1))

        if [[ $line_num -eq 1 ]]; then
            if [[ "$line" == "---" ]]; then
                in_front_matter=1
                echo "$line"
                continue
            else
                # No front matter in this file
                echo "(no YAML front matter found)"
                return
            fi
        fi

        if [[ $in_front_matter -eq 1 ]]; then
            echo "$line"
            if [[ "$line" == "---" ]]; then
                return
            fi
        fi
    done < "$filepath"

    # If we got here, front matter was never closed
    echo "(warning: front matter not closed)"
}

while IFS= read -r file; do
    # Skip the export script itself, the output file, and anything in docs/
    if [[ "$file" == "export.sh" || "$file" == "$OUTPUT_FILE" || "$file" == docs/* ]]; then
        continue
    fi

    # Check if file is text-based (skip binary files like images, fonts, etc.)
    if ! file "$file" | grep -qE 'text|JSON|XML|yaml|empty'; then
        echo "⏩ Skipping binary: $file"
        SKIPPED_COUNTER=$((SKIPPED_COUNTER + 1))
        continue
    fi

    # Blog posts: front matter only
    if [[ "$file" == content/blog/*.md ]]; then
        echo "📝 Adding (front matter only): $file"
        {
            echo "-------------------------------------------------------------------------------"
            echo "FILE: $file  [FRONT MATTER ONLY — body omitted to save context space]"
            echo "-------------------------------------------------------------------------------"
            extract_front_matter "$file"
            echo ""
            echo ""
        } >> "$OUTPUT_FILE"
        COUNTER=$((COUNTER + 1))
        BLOG_COUNTER=$((BLOG_COUNTER + 1))
    else
        # Everything else: full content
        echo "📄 Adding: $file"
        {
            echo "-------------------------------------------------------------------------------"
            echo "FILE: $file"
            echo "-------------------------------------------------------------------------------"
            cat "$file"
            echo ""
            echo ""
        } >> "$OUTPUT_FILE"
        COUNTER=$((COUNTER + 1))
    fi
done < <(git ls-files)

# ─────────────────────────────────────────────────────────────────────────────
# Footer summary
# ─────────────────────────────────────────────────────────────────────────────
{
    echo "==============================================================================="
    echo "EXPORT COMPLETED"
    echo "Total Files Exported: $COUNTER"
    echo "  Blog posts (front matter only): $BLOG_COUNTER"
    echo "  Full content files: $((COUNTER - BLOG_COUNTER))"
    echo "  Binary files skipped: $SKIPPED_COUNTER"
    echo "Output File: $OUTPUT_FILE"
    echo "==============================================================================="
} >> "$OUTPUT_FILE"

echo ""
echo "✅ Export complete!"
echo "   $COUNTER files written to $OUTPUT_FILE"
echo "   $BLOG_COUNTER blog posts (front matter only)"
echo "   $((COUNTER - BLOG_COUNTER)) full content files"
echo "   $SKIPPED_COUNTER binary files skipped"

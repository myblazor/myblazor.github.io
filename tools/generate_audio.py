#!/usr/bin/env python3
"""
Generate MP3 audio files from blog post markdown using KittenTTS.

Usage:
    python tools/generate_audio.py --content-dir content/blog --output-dir src/ObserverMagazine.Web/wwwroot/blog-data

Requires:
    - KittenTTS 0.8.1 (pip install from GitHub releases)
    - espeak-ng (apt install espeak-ng)
    - ffmpeg (for WAV → MP3 conversion)
    - soundfile, num2words

The script:
    1. Reads each .md file from content-dir
    2. Strips YAML front matter and markdown formatting → plain text
    3. Generates speech audio with KittenTTS (nano model, CPU-only, ~25MB)
    4. Converts WAV → MP3 at 64kbps mono via ffmpeg (keeps files small)
    5. Skips regeneration if the MP3 is already newer than the .md source
"""

import argparse
import os
import re
import subprocess
import sys
import tempfile
import time

# ---------------------------------------------------------------------------
# Text extraction
# ---------------------------------------------------------------------------

def strip_front_matter(content: str) -> str:
    """Remove YAML front matter delimited by --- ... ---"""
    if not content.startswith("---"):
        return content
    end = content.find("---", 3)
    if end < 0:
        return content
    return content[end + 3:].strip()


def strip_markdown(text: str) -> str:
    """Convert markdown to plain text suitable for TTS."""
    # Remove code blocks
    text = re.sub(r"```[\s\S]*?```", " ", text)
    text = re.sub(r"`[^`]+`", " ", text)

    # Remove images
    text = re.sub(r"!\[.*?\]\(.*?\)", " ", text)

    # Convert links to just their text
    text = re.sub(r"\[([^\]]+)\]\([^)]+\)", r"\1", text)

    # Remove headers (keep the text)
    text = re.sub(r"^#{1,6}\s+", "", text, flags=re.MULTILINE)

    # Remove bold/italic markers
    text = re.sub(r"\*{1,3}([^*]+)\*{1,3}", r"\1", text)
    text = re.sub(r"_{1,3}([^_]+)_{1,3}", r"\1", text)

    # Remove horizontal rules
    text = re.sub(r"^[-*_]{3,}\s*$", " ", text, flags=re.MULTILINE)

    # Remove HTML tags
    text = re.sub(r"<[^>]+>", " ", text)

    # Remove blockquote markers
    text = re.sub(r"^>\s*", "", text, flags=re.MULTILINE)

    # Remove list markers
    text = re.sub(r"^[\s]*[-*+]\s+", "", text, flags=re.MULTILINE)
    text = re.sub(r"^[\s]*\d+\.\s+", "", text, flags=re.MULTILINE)

    # Collapse whitespace
    text = re.sub(r"\n{2,}", ". ", text)
    text = re.sub(r"\n", " ", text)
    text = re.sub(r"\s{2,}", " ", text)

    return text.strip()


def preprocess_programming_terms(text: str) -> str:
    """
    Replace programming terms and symbols that TTS engines commonly mispronounce.
    Must run BEFORE preprocess_numbers since some terms contain digits.
    """
    replacements = [
        # Languages and frameworks — order matters (longer matches first)
        (r"\.NET\s+10", "dot net ten"),
        (r"\.NET\s+8", "dot net eight"),
        (r"\.NET\s+7", "dot net seven"),
        (r"\.NET\s+6", "dot net six"),
        (r"\.NET\s+Core\s+3\.0", "dot net core three point oh"),
        (r"\.NET\s+Core", "dot net core"),
        (r"\.NET\s+Framework\s+4\.8", "dot net framework four point eight"),
        (r"\.NET\s+Framework", "dot net framework"),
        (r"\.NET", "dot net"),
        (r"\bC#", "C sharp"),
        (r"\bF#", "F sharp"),
        (r"\bC\+\+", "C plus plus"),
        (r"\bASP\.NET", "A S P dot net"),

        # File extensions and config
        (r"\.csproj\b", " C sharp project file"),
        (r"\.cshtml\b", " C S H T M L"),
        (r"\.ascx\b", " A S C X"),
        (r"\.aspx\b", " A S P X"),
        (r"\.slnx\b", " solution X"),
        (r"\.sln\b", " solution"),
        (r"\.json\b", " JSON"),
        (r"\.yml\b", " YAML"),
        (r"\.xml\b", " X M L"),
        (r"\.md\b", " markdown"),
        (r"\.css\b", " C S S"),
        (r"\.js\b", " JavaScript"),
        (r"\.wasm\b", " web assembly"),

        # Common abbreviations
        (r"\bWASM\b", "web assembly"),
        (r"\bIL\b", "intermediate language"),
        (r"\bCLR\b", "common language runtime"),
        (r"\bJIT\b", "just in time"),
        (r"\bAOT\b", "ahead of time"),
        (r"\bNGen\b", "N gen"),
        (r"\bR2R\b", "ready to run"),
        (r"\bDI\b", "dependency injection"),
        (r"\bOWIN\b", "oh win"),
        (r"\bCORS\b", "cross origin resource sharing"),
        (r"\bRSS\b", "R S S"),
        (r"\bAPI\b", "A P I"),
        (r"\bAPIs\b", "A P Is"),
        (r"\bUI\b", "U I"),
        (r"\bUIs\b", "U Is"),
        (r"\bURL\b", "U R L"),
        (r"\bHTTP\b", "H T T P"),
        (r"\bHTTPS\b", "H T T P S"),
        (r"\bHTML\b", "H T M L"),
        (r"\bCSS\b", "C S S"),
        (r"\bSQL\b", "S Q L"),
        (r"\bSSD\b", "S S D"),
        (r"\bSSDs\b", "S S Ds"),
        (r"\bI/O\b", "I O"),
        (r"\bIIS\b", "I I S"),
        (r"\bMVC\b", "M V C"),
        (r"\bLTS\b", "long term support"),
        (r"\bSDK\b", "S D K"),
        (r"\bNuGet\b", "new get"),
        (r"\bxUnit\b", "x unit"),
        (r"\bbUnit\b", "b unit"),

        # Symbols in technical context
        (r"=>", " arrow "),
        (r"!=", " not equal "),
        (r"==", " equals "),
        (r">=", " greater than or equal "),
        (r"<=", " less than or equal "),

        # Version patterns (e.g., "v3", "v6")
        (r"\bv(\d+)\b", r"version \1"),
    ]

    for pattern, replacement in replacements:
        text = re.sub(pattern, replacement, text)

    return text


def preprocess_numbers(text: str) -> str:
    """Convert numbers to words to work around KittenTTS number pronunciation bug."""
    try:
        from num2words import num2words as n2w
    except ImportError:
        print("WARNING: num2words not installed, skipping number conversion")
        return text

    def replace_number(match):
        num_str = match.group(0)
        try:
            # Handle decimals
            if "." in num_str:
                return n2w(float(num_str))
            return n2w(int(num_str))
        except (ValueError, OverflowError):
            return num_str

    # Match numbers (including decimals), but not parts of words
    return re.sub(r"\b\d+\.?\d*\b", replace_number, text)


def derive_slug(filename: str) -> str:
    """Derive slug from filename: 2026-01-15-welcome-post.md → welcome-post"""
    name = os.path.splitext(filename)[0]
    if (len(name) > 11
            and name[4] == "-"
            and name[7] == "-"
            and name[10] == "-"
            and name[:4].isdigit()):
        return name[11:]
    return name


# ---------------------------------------------------------------------------
# Audio generation
# ---------------------------------------------------------------------------

def chunk_text(text: str, max_chars: int = 500) -> list[str]:
    """
    Split text into chunks for TTS processing.
    KittenTTS works best with shorter segments.
    Splits on sentence boundaries (. ! ?) to maintain natural speech flow.
    """
    sentences = re.split(r"(?<=[.!?])\s+", text)
    chunks = []
    current = ""

    for sentence in sentences:
        if not sentence.strip():
            continue
        if len(current) + len(sentence) + 1 > max_chars and current:
            chunks.append(current.strip())
            current = sentence
        else:
            current = f"{current} {sentence}" if current else sentence

    if current.strip():
        chunks.append(current.strip())

    return chunks if chunks else [text]


def generate_audio(text: str, output_mp3: str, voice: str = "Bella", model_name: str = "KittenML/kitten-tts-nano-0.8") -> bool:
    """
    Generate MP3 audio from text using KittenTTS.

    Steps:
        1. Chunk text into TTS-friendly segments
        2. Generate WAV audio for each chunk
        3. Concatenate chunks
        4. Convert combined WAV → MP3 via ffmpeg at 64kbps mono

    Returns True on success, False on failure.
    """
    import numpy as np
    import soundfile as sf

    try:
        from kittentts import KittenTTS
    except ImportError:
        print("ERROR: kittentts not installed. Run: pip install -r tools/requirements-audio.txt")
        return False

    print(f"  Loading model: {model_name}")
    start = time.time()
    model = KittenTTS(model_name)
    print(f"  Model loaded in {time.time() - start:.1f}s")

    chunks = chunk_text(text)
    print(f"  Processing {len(chunks)} text chunk(s), voice={voice}")

    all_audio = []
    for i, chunk in enumerate(chunks):
        if not chunk.strip():
            continue
        print(f"    Chunk {i+1}/{len(chunks)}: {len(chunk)} chars")
        try:
            audio = model.generate(chunk, voice=voice, speed=1.0)
            all_audio.append(audio)
            # Add a short silence between chunks (0.3s at 24kHz)
            all_audio.append(np.zeros(int(24000 * 0.3), dtype=np.float32))
        except Exception as e:
            print(f"    WARNING: Failed to generate chunk {i+1}: {e}")
            continue

    if not all_audio:
        print("  ERROR: No audio generated")
        return False

    combined = np.concatenate(all_audio)
    duration_sec = len(combined) / 24000
    print(f"  Audio duration: {duration_sec:.1f}s")

    # Write temporary WAV
    with tempfile.NamedTemporaryFile(suffix=".wav", delete=False) as tmp:
        tmp_wav = tmp.name
        sf.write(tmp_wav, combined, 24000)

    try:
        # Convert to MP3 using ffmpeg: 64kbps mono (good quality for speech, small file)
        os.makedirs(os.path.dirname(output_mp3), exist_ok=True)
        result = subprocess.run(
            [
                "ffmpeg", "-y",
                "-i", tmp_wav,
                "-codec:a", "libmp3lame",
                "-b:a", "64k",
                "-ac", "1",          # mono
                "-ar", "24000",      # keep original sample rate
                output_mp3,
            ],
            capture_output=True,
            text=True,
            timeout=120,
        )

        if result.returncode != 0:
            print(f"  ERROR: ffmpeg failed:\n{result.stderr}")
            return False

        mp3_size = os.path.getsize(output_mp3)
        mp3_size_mb = mp3_size / (1024 * 1024)
        print(f"  Wrote: {output_mp3} ({mp3_size_mb:.2f} MB, {duration_sec:.1f}s)")

        if mp3_size_mb > 40:
            print(f"  WARNING: File exceeds 40MB limit! ({mp3_size_mb:.1f} MB)")

        return True

    finally:
        os.unlink(tmp_wav)


# ---------------------------------------------------------------------------
# Main
# ---------------------------------------------------------------------------

def main():
    parser = argparse.ArgumentParser(description="Generate TTS audio for blog posts")
    parser.add_argument("--content-dir", default="content/blog", help="Markdown source directory")
    parser.add_argument("--output-dir", default="src/ObserverMagazine.Web/wwwroot/blog-data", help="Output directory for MP3 files")
    parser.add_argument("--voice", default="Bella", help="KittenTTS voice (Bella, Jasper, Luna, Bruno, Rosie, Hugo, Kiki, Leo)")
    parser.add_argument("--model", default="KittenML/kitten-tts-nano-0.8",
                        help="HuggingFace model ID (nano=25MB/fast, mini=80MB/better quality)")
    parser.add_argument("--force", action="store_true", help="Regenerate even if MP3 is up to date")
    args = parser.parse_args()

    if not os.path.isdir(args.content_dir):
        print(f"Content directory not found: {args.content_dir}")
        sys.exit(1)

    os.makedirs(args.output_dir, exist_ok=True)

    md_files = sorted(f for f in os.listdir(args.content_dir) if f.endswith(".md"))
    print(f"Found {len(md_files)} markdown file(s) in {args.content_dir}")

    generated = 0
    skipped = 0
    failed = 0

    for md_file in md_files:
        md_path = os.path.join(args.content_dir, md_file)
        slug = derive_slug(md_file)
        mp3_path = os.path.join(args.output_dir, f"{slug}.mp3")

        # Skip if MP3 is up to date
        if not args.force and os.path.exists(mp3_path):
            md_mtime = os.path.getmtime(md_path)
            mp3_mtime = os.path.getmtime(mp3_path)
            if mp3_mtime >= md_mtime:
                print(f"  Skipping (up to date): {slug}.mp3")
                skipped += 1
                continue

        print(f"\nProcessing: {md_file} → {slug}.mp3")

        raw = open(md_path, encoding="utf-8").read()
        body = strip_front_matter(raw)
        text = strip_markdown(body)
        text = preprocess_programming_terms(text)  # MUST run before preprocess_numbers
        text = preprocess_numbers(text)

        if len(text.strip()) < 10:
            print(f"  Skipping (too short): {len(text)} chars")
            skipped += 1
            continue

        print(f"  Text: {len(text)} chars")

        start = time.time()
        if generate_audio(text, mp3_path, voice=args.voice, model_name=args.model):
            elapsed = time.time() - start
            print(f"  Done in {elapsed:.1f}s")
            generated += 1
        else:
            print(f"  FAILED: {slug}")
            failed += 1

    print(f"\nAudio generation complete: {generated} generated, {skipped} skipped, {failed} failed")

    if failed > 0:
        sys.exit(1)


if __name__ == "__main__":
    main()

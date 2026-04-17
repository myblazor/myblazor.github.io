---
title: "Without a Net, Part 12: Forms, Validation, and the Constraint Validation API"
date: 2026-06-04
author: myblazor-team
summary: "Day 12 of our fifteen-part no-build web series covers forms — the most important and most under-taught part of the web platform. We cover every input type that has appeared in the last decade, the Constraint Validation API that has been there since 2010 and that almost nobody reads about, the pseudo-classes that make CSS-only error styling work properly, setCustomValidity for arbitrary rules, the new requestSubmit() method, async server-side validation done right, accessible error messages with aria-describedby and aria-invalid, autocomplete tokens that actually help users, and a complete worked sign-up form with everything wired up. If you have ever reached for Formik or React Hook Form because you needed validation, today is the day you stop."
tags:
  - html
  - forms
  - validation
  - constraint-validation
  - accessibility
  - aria
  - autocomplete
  - no-build
  - first-principles
  - series
  - deep-dive
---

## Introduction: The 800-Line Sign-Up Form

A few years ago we were brought in to audit a checkout flow for a small commerce site. The team had been told their conversion rate was 30% lower than industry benchmarks, and they could not figure out why. The CTO walked us through the tech: React, TypeScript, Formik for forms, Yup for validation schemas, Axios for the API, a custom internationalisation layer, a dozen other reasonable choices. The codebase was clean. The engineering was good. The product worked.

We sat with a designer for an hour and watched real users try to complete the checkout form on their phones. Three people in a row gave up at the same point — the credit-card-number field. We watched the fourth user. He typed his card number. The form rejected it. He retyped it. Same rejection. He squinted at the form and gave up.

The reason: the team had built a custom credit-card-number input that did not have `inputmode="numeric"` or `autocomplete="cc-number"`. On the user's phone, the keyboard came up in QWERTY mode. He had typed the number using the wrong-shaped keyboard, and the input had not normalised the spaces, and the validation was strict, and the error message said "invalid card number" without explaining what specifically was wrong. The native browser autofill — which would have filled the card number in correctly with one tap — was not offered, because the autocomplete attribute was missing, so the browser did not recognise it as a credit-card field.

We added `inputmode="numeric"` and `autocomplete="cc-number"` to the input. Two attributes. Conversion rate went up about 18 percentage points within a week. The form code, the Formik state machine, the Yup schema, the validation logic — all of it was working correctly. The native form features the team had not used were what was missing. The custom code was hiding the platform.

This is the story of forms in a depressing number of modern projects. Teams reach for `Formik` or `React Hook Form` or `VeeValidate` because they "need form validation." The library handles it, sort of, but the implementation usually skips half the platform features that make forms actually pleasant to use — autocomplete tokens, `inputmode`, the right input types, the Constraint Validation API, accessible error messaging. The form library is doing more work than the browser would have done for free, and doing it less well.

Day 12 is our complete tour of the form layer. We will start with input types — the surprising number of them and the surprising helpfulness of choosing the right one. We will cover the autocomplete tokens that turn forms into one-tap experiences for returning users. We will dig into the Constraint Validation API, which has been in the platform since HTML5 and which most developers have never read the spec for. We will cover `:user-valid` and `:user-invalid`, the pseudo-classes that finally make CSS-only validation styling work properly. We will cover `setCustomValidity` for arbitrary rules and async server checks. We will cover the new `requestSubmit()` method that is finally a clean way to programmatically submit a form. And we will end with a complete sign-up form with everything wired up — about 80 lines of HTML, 20 lines of CSS, 30 lines of JavaScript, fully accessible, fully validated, ready to ship.

This is Part 12 of 15 in our no-build web series. After [Day 11's routing](/blog/2026-06-03-no-build-web-routing-and-navigation), we have a way to navigate between pages. Today we cover what users do *on* those pages.

## Part 1: Input Types — Choose The Right Tool

The `<input>` element has 22 type values. Most developers use about four. Using the right one is the single biggest free improvement you can make to a form's usability. Each type:

- Sets the appropriate keyboard on mobile.
- Activates relevant browser autofill.
- Provides built-in validation.
- Sometimes provides a built-in UI (date picker, colour picker).
- Conveys semantic meaning to assistive technologies.

The full list:

| Type | Use for | Notes |
|---|---|---|
| `text` | Generic text | Default. Use only when nothing more specific applies. |
| `email` | Email addresses | Validates format. Mobile keyboard has `@` key. |
| `tel` | Phone numbers | Mobile keyboard is numeric/dialler. No format validation (intentional). |
| `url` | Web URLs | Validates format. Keyboard has `/` and `.` keys. |
| `search` | Search queries | On some browsers shows a clear-input "X" button. |
| `password` | Passwords | Masked input. Triggers password-manager UI. |
| `number` | Numeric values | Spinner UI. Validates with `min`/`max`/`step`. |
| `range` | Numeric slider | Drag UI. |
| `date` | Calendar date | Native date picker. |
| `time` | Time of day | Native time picker. |
| `datetime-local` | Date and time | Combined picker. |
| `month` | Year and month | Picker. |
| `week` | Year and week | Picker (rarely useful). |
| `color` | Colour | Native colour picker. |
| `file` | File upload | Native file picker. |
| `checkbox` | Boolean toggle | |
| `radio` | One-of-many | Group with `name`. |
| `hidden` | Hidden value | Submitted with form, not displayed. |
| `submit` | Submit button | Use `<button type="submit">` instead — more flexible. |
| `reset` | Reset button | Almost never useful. Avoid. |
| `image` | Submit-with-coordinates | Almost never useful. Avoid. |
| `button` | Generic button | Use `<button>` instead. |

The big ones to know that often get ignored:

**`type="email"`**. Validates format (sort of — accepts technically-valid edge cases that real services reject; consider it "is there an @"). The mobile keyboard adds `@`. Triggers email autofill.

**`type="tel"`**. Crucial. Brings up the dialler keyboard on mobile. Does not validate (because phone formats vary too much globally), so pair with `pattern="[0-9 ()+-]*"` if you want to constrain it. Triggers phone-number autofill.

**`type="number"`**. Brings up the numeric keyboard. Has spinners on desktop. Allows `min`, `max`, `step` constraints. Caveat: it strips leading zeros, which is wrong for things like postal codes. For "values that look like numbers but are not arithmetic" (zip codes, card numbers, OTP codes), use `type="text" inputmode="numeric"` instead.

**`inputmode` attribute**. Independent of `type`, `inputmode` controls the mobile keyboard layout. Values: `"text"`, `"numeric"`, `"decimal"`, `"tel"`, `"email"`, `"url"`, `"search"`, `"none"`. Use `type="text" inputmode="numeric"` for "numeric-looking strings" (postal codes, OTP codes), `type="text" inputmode="decimal"` for currency amounts.

**`type="search"`**. Adds an "X" clear button on some browsers (Chrome/Edge), uses an appropriately-styled keyboard, and tells assistive tech "this is a search input." Do not use plain `text` for search.

**`type="date"`** and friends. The native date picker is **vastly** better than the JavaScript ones we used to ship. It uses the user's locale, supports keyboard input, supports screen readers, supports keyboard-only navigation, and is touch-friendly on mobile. Use it. The valid value format is always `YYYY-MM-DD` regardless of the user's locale — the browser handles the display.

The catch: native date pickers used to be impossible to style consistently. As of 2025, `appearance: base` (the new "Basic Appearance" CSS feature) lets you style them — see Part 6 of [Day 6](/blog/2026-05-29-no-build-web-color-typography-motion). Until that ships in every browser, you live with the native style.

## Part 2: Autocomplete Tokens — The Free Conversion Boost

The `autocomplete` attribute is one of the highest-ROI features in web development, in our experience. Adding the right autocomplete token to your inputs lets the browser autofill them with one tap from saved data — the user's name, address, payment details, anything. For returning users completing a form on mobile, this can mean filling out the form in two seconds instead of two minutes.

The values are a controlled vocabulary. The full list is in [the WHATWG spec](https://html.spec.whatwg.org/multipage/form-control-infrastructure.html#autofill); the most useful subset:

```
name, given-name, family-name, additional-name, honorific-prefix, honorific-suffix, nickname,
email, username, new-password, current-password, one-time-code,
organization-title, organization,
street-address, address-line1, address-line2, address-line3,
address-level1 (state), address-level2 (city), address-level3, address-level4,
country, country-name, postal-code,
cc-name, cc-given-name, cc-family-name, cc-number, cc-exp, cc-exp-month, cc-exp-year,
cc-csc, cc-type,
bday, bday-day, bday-month, bday-year,
sex, language,
tel, tel-country-code, tel-national, tel-area-code, tel-local, tel-extension,
url, photo
```

Examples:

```html
<input type="email"    autocomplete="email">
<input type="text"     autocomplete="given-name"  name="first">
<input type="text"     autocomplete="family-name" name="last">
<input type="tel"      autocomplete="tel">
<input type="text"     autocomplete="street-address" name="address">
<input type="text"     autocomplete="postal-code" inputmode="numeric" name="zip">
<input type="password" autocomplete="new-password" name="password">
<input type="password" autocomplete="current-password" name="password">
<input type="text"     autocomplete="one-time-code" inputmode="numeric" name="otp">
```

Two of these deserve special mention:

**`autocomplete="new-password"`** vs **`autocomplete="current-password"`**. On a sign-up form, use `new-password`, which triggers the browser's password-suggestion UI. On a sign-in form, use `current-password`, which suggests the saved password. Mixing them up means the browser will sometimes prompt for a new strong password when the user is trying to sign in — a frustrating bug we have seen on more than one production site.

**`autocomplete="one-time-code"`**. On iOS and increasingly Android, this triggers the "from messages" suggestion when an SMS verification code arrives. The user taps the suggestion above their keyboard and the code fills in. Adding this attribute to your OTP input is a thirty-character change with a measurable conversion impact.

For checkout flows, the autocomplete tokens for credit-card fields (`cc-number`, `cc-exp`, `cc-csc`) trigger the browser's saved-card UI. On mobile Safari and Chrome, this is essentially the difference between users completing the purchase or abandoning. Use them.

The `autocomplete` value can be prefixed with a "section" to disambiguate when the same kind of value appears multiple times (shipping vs. billing address):

```html
<input autocomplete="section-shipping street-address">
<input autocomplete="section-billing street-address">
```

Browsers use the section name to keep the saved values straight.

To explicitly disable autofill for a sensitive field:

```html
<input autocomplete="off">
```

But beware — modern browsers sometimes ignore `autocomplete="off"` because users complain that they want autofill anyway. Do not rely on it for security; for genuine-secret inputs (one-time codes, MFA backup codes) use the appropriate semantic value (`one-time-code`) instead.

## Part 3: Native Constraints

The browser ships built-in validation rules attached to the relevant input types and attributes:

- **`required`** — must have a value.
- **`minlength="N"`** — minimum string length (after the user has typed).
- **`maxlength="N"`** — maximum string length (enforced as the user types).
- **`min="N"`** and **`max="N"`** — for `number`, `range`, `date`, `time`. Inclusive.
- **`step="N"`** — for `number`, `range`, `date`. The value must be a multiple.
- **`pattern="regex"`** — must match the regex (for `text`, `tel`, `password`, etc.).
- **`type="email"`** with **`multiple`** — comma-separated email list.

Each of these maps to a `ValidityState` flag, which we will see in Part 4.

A simple example:

```html
<form id="signup">
  <label>
    Email
    <input type="email" name="email" autocomplete="email" required>
  </label>
  <label>
    Password
    <input type="password" name="password"
           autocomplete="new-password"
           required minlength="8">
  </label>
  <label>
    Date of birth
    <input type="date" name="dob" required
           min="1900-01-01" max="2008-01-01">
  </label>
  <button type="submit">Create account</button>
</form>
```

That form has validation:

- Email is required and must be a valid email format.
- Password is required and must be at least 8 characters.
- Date of birth is required and must be between 1900-01-01 and 2008-01-01.

If the user clicks "Create account" without filling these correctly, the browser shows native validation messages and prevents submission. No JavaScript. No library. The only validation library you need for the basic cases is HTML5.

### `pattern` — the regex constraint

For anything more specific than the built-in formats, use `pattern`:

```html
<!-- US zip code -->
<input type="text" inputmode="numeric"
       autocomplete="postal-code"
       pattern="\d{5}(-\d{4})?"
       title="5 digits, optionally with a 4-digit extension">

<!-- UK postcode (rough) -->
<input type="text"
       autocomplete="postal-code"
       pattern="[A-Z]{1,2}[0-9][A-Z0-9]? ?[0-9][A-Z]{2}"
       title="A UK postcode like SW1A 1AA">

<!-- Username (alphanumeric + underscore, 3-20 chars) -->
<input type="text"
       pattern="[a-zA-Z0-9_]{3,20}"
       title="3-20 letters, numbers, or underscores">
```

The `title` attribute is what the browser shows next to the validation message. Without it, you get a generic "Please match the requested format" message — describing the constraint in plain English with `title` is significantly more helpful.

The pattern is anchored at start and end automatically (no need for `^` and `$`). It is JavaScript regex syntax, with the `v` flag (Unicode-aware).

### `minlength` and password strength

A common mistake: using `minlength` to validate password strength.

```html
<!-- Inadequate -->
<input type="password" required minlength="8">
```

`minlength="8"` allows `"password"` and `"12345678"`. For real strength validation, use `pattern` with character-class requirements or — better — accept any reasonable length and rely on the user's password manager to generate a strong one. The current cybersecurity consensus (and the [NIST guidance](https://pages.nist.gov/800-63-3/sp800-63b.html)) is that **length is more important than complexity**, and **strict complexity rules tend to make things worse** by encouraging predictable substitutions (`P@ssw0rd!`).

Our recommendation: `minlength="12"` and no other constraint. Trust users to use a password manager.

## Part 4: The Constraint Validation API

The Constraint Validation API is the JavaScript interface to the validation system. It has been in the platform since [HTML5 was finalised in 2014](https://www.w3.org/TR/html5/forms.html#the-constraint-validation-api). Most developers have never used it directly because their framework wraps it. Let us look at what it actually offers.

Every form control element has these properties and methods:

- **`element.validity`** — a `ValidityState` object with boolean flags.
- **`element.validationMessage`** — the localised error message string.
- **`element.willValidate`** — `true` if the element will be validated.
- **`element.checkValidity()`** — returns `true` if the element is valid; fires an `invalid` event if not.
- **`element.reportValidity()`** — same, but also displays the browser's UI for showing the error.
- **`element.setCustomValidity(message)`** — set a custom error. Pass `""` to clear.

And `<form>` elements:

- **`form.checkValidity()`** — checks all controls.
- **`form.reportValidity()`** — checks and shows UI for all controls.
- **`form.requestSubmit()`** — programmatically submit, running validation first.

### `ValidityState` flags

```javascript
const input = document.querySelector('input[type=email]');
console.log(input.validity);
// {
//   valid: false,
//   valueMissing: false,
//   typeMismatch: true,
//   patternMismatch: false,
//   tooLong: false,
//   tooShort: false,
//   rangeUnderflow: false,
//   rangeOverflow: false,
//   stepMismatch: false,
//   badInput: false,
//   customError: false,
// }
```

Each flag corresponds to a specific failure mode. `valueMissing` is `required` violated. `typeMismatch` is the value not matching the type's format (e.g., `email` without `@`). `patternMismatch` is the `pattern` regex violated. `tooLong` and `tooShort` are `maxlength`/`minlength`. `rangeUnderflow`/`rangeOverflow`/`stepMismatch` are for numeric/date inputs. `badInput` is for inputs that cannot parse what was typed (e.g., letters in a `number` field). `customError` is set by `setCustomValidity`.

### The `invalid` event

```javascript
input.addEventListener("invalid", (event) => {
  // The browser is about to show the native validation popup.
  // event.preventDefault() suppresses it — you can show your own UI instead.
  console.log("Validity:", input.validity);
});
```

Fires when validation fails. Useful for logging analytics on validation failures (which fields users fail most often), or for replacing the browser's default popup with custom UI.

## Part 5: `:user-valid` And `:user-invalid` — Styling Done Right

The original `:valid` and `:invalid` pseudo-classes had a usability problem: they applied immediately on page load. A required field without a value is `:invalid` from the moment the page renders, so a CSS rule like `input:invalid { border: 2px solid red; }` paints every required field red before the user has done anything. This is bad UX — users hate being told they made a mistake before they have had a chance to make one.

For years, the workaround was JavaScript. Track which fields the user had interacted with (focus + blur, or a "touched" flag), then conditionally apply the error style. Every form library does this internally.

[`:user-valid` and `:user-invalid` became Baseline Newly Available in November 2023](https://web.dev/articles/user-valid-and-user-invalid-pseudo-classes). They behave like `:valid`/`:invalid` but only after the user has interacted meaningfully — typed in the field, blurred it, or attempted to submit the form. Until then, the field is in a neutral state.

```css
input:user-invalid {
  border-color: oklch(55% 0.2 25);
  outline: 2px solid oklch(55% 0.2 25 / 0.2);
}

input:user-valid {
  border-color: oklch(55% 0.15 145);
}
```

That is the entire validation styling. No JavaScript. No "touched" tracking. Errors appear when the user has actually demonstrated a problem, not before.

The behaviour:

- Field is `:user-invalid` after the user has entered text and blurred the field, or attempted to submit the form.
- Field is `:user-valid` once a previously-`:user-invalid` field becomes valid.
- Field is neither immediately on page load.
- Once the form is submitted (and fails), all `:invalid` fields become `:user-invalid` until the user fixes them.

This matches the UX you want: "tell me what is wrong only after I have given you something to evaluate."

### Pairing with custom error messages

The native validation popup is functional but cannot be styled. Many designers want their own error message UI. The pattern combines `:user-invalid` with a sibling element:

```html
<label>
  Email
  <input type="email" name="email" required
         aria-describedby="email-error">
  <span id="email-error" class="error-message">
    Please enter a valid email address.
  </span>
</label>
```

```css
.error-message {
  color: oklch(55% 0.2 25);
  font-size: 0.875em;
  display: none;
}

input:user-invalid {
  border-color: oklch(55% 0.2 25);
}

input:user-invalid + .error-message {
  display: block;
}
```

The error message is hidden by default. When the input is `:user-invalid`, the message appears. `aria-describedby` ties the message to the input so that screen readers announce it when the input is focused. Pure CSS solution, fully accessible, no JavaScript.

To suppress the browser's native popup as well — keeping only your custom message — add `novalidate` to the form, then call `form.checkValidity()` yourself in the submit handler. We will see the full pattern in Part 9 below.

### `:has()` — form-level validation styling

With CSS `:has()` (Day 5), you can react to invalid fields *anywhere* in the form:

```css
form:has(:user-invalid) button[type="submit"] {
  opacity: 0.6;
  cursor: not-allowed;
}
```

The submit button visually disables itself when any field is invalid. (Note: visually-only — also use `aria-disabled` or `disabled` for actual disabled behaviour, see Part 9.)

## Part 6: `setCustomValidity` — Arbitrary Rules

Native constraints cover most cases. For everything else — "passwords must contain a number," "username must be unique on our server," "this date must be after the previous one" — use `setCustomValidity`.

```javascript
const password = document.querySelector('input[name=password]');
const confirm = document.querySelector('input[name=confirm]');

confirm.addEventListener("input", () => {
  if (confirm.value !== password.value) {
    confirm.setCustomValidity("Passwords do not match.");
  } else {
    confirm.setCustomValidity("");   // empty string = valid
  }
});
```

That is the entire pattern:

- Call `setCustomValidity("error message")` to mark invalid with that message.
- Call `setCustomValidity("")` to clear the error.

The element's `validity.customError` flag becomes `true` and the validation message becomes the string you provided. Native validation UI shows your message. `:user-invalid` matches. Form submission is blocked. All the same machinery, with your custom rule.

For multi-field rules (the "passwords must match" case), wire the listener to whichever field's value triggers the check. For `confirm`-style fields, react when either field changes.

### A more complex example — date validation

A trip form with start and end dates:

```html
<form>
  <label>Departure: <input type="date" name="depart" required></label>
  <label>Return: <input type="date" name="return" required></label>
  <button type="submit">Book</button>
</form>
```

```javascript
const form = document.querySelector("form");
const depart = form.elements.depart;
const ret = form.elements.return;

function validateDates() {
  if (depart.value && ret.value && ret.value < depart.value) {
    ret.setCustomValidity("Return date must be after departure.");
  } else {
    ret.setCustomValidity("");
  }
}

depart.addEventListener("change", validateDates);
ret.addEventListener("change", validateDates);
```

Two rules, four lines each. Done.

### Mind the message reset

If you have multiple rules on the same field, you must clear the custom validity each time you check, otherwise an old error sticks:

```javascript
function validate(input) {
  input.setCustomValidity("");   // clear first

  if (someCondition) {
    input.setCustomValidity("Error message");
  } else if (otherCondition) {
    input.setCustomValidity("Other error message");
  }
}
```

Without the leading clear, a previously-set error survives even when its condition no longer applies. This is the most common bug we see with `setCustomValidity`.

## Part 7: Async Validation — Server-Side Rules

Some validation cannot run on the client. "Is this username already taken?" requires a server round-trip. The pattern:

```javascript
import { debounce } from "@app/utils/dom.js";

const username = document.querySelector('input[name=username]');

const checkUsername = debounce(async () => {
  if (!username.value) {
    username.setCustomValidity("");
    return;
  }
  username.setCustomValidity("Checking...");
  try {
    const response = await fetch(`/api/check-username?u=${encodeURIComponent(username.value)}`);
    const { taken } = await response.json();
    username.setCustomValidity(taken ? "This username is taken." : "");
  } catch {
    username.setCustomValidity("");   // network error: do not block submit
  }
}, 400);

username.addEventListener("input", checkUsername);
```

Debounced 400ms (no checking on every keystroke). While the request is in flight, `setCustomValidity("Checking...")` blocks form submission with a sensible message. On success, set the right error or clear it. On network error, *clear* the error — better to let the user submit and let the server reject than to block them indefinitely.

Pair with a tiny visual indicator near the input:

```html
<label>
  Username
  <input type="text" name="username" required
         minlength="3" pattern="[a-zA-Z0-9_]+">
  <span class="status" aria-live="polite"></span>
</label>
```

```javascript
username.addEventListener("input", () => {
  const status = username.parentElement.querySelector(".status");
  status.textContent = username.value ? "Checking..." : "";
});
```

The `aria-live="polite"` region announces the status to screen-reader users. Combined with the `:user-invalid` styling and `setCustomValidity`, you have async server validation, fully accessible, in about 25 lines.

### Race conditions

If the user types quickly, multiple requests may be in flight. The naive code above could resolve them out of order — the response for `"jo"` might arrive after the response for `"john"`, leaving the wrong validity. Use `AbortController` (Day 8):

```javascript
let controller;

const checkUsername = debounce(async () => {
  controller?.abort();
  controller = new AbortController();
  const { signal } = controller;
  try {
    const response = await fetch(url, { signal });
    if (signal.aborted) return;
    const { taken } = await response.json();
    username.setCustomValidity(taken ? "Taken." : "");
  } catch (e) {
    if (e.name !== "AbortError") username.setCustomValidity("");
  }
}, 400);
```

Each new check aborts the previous one. Only the latest result wins. This is the same pattern from [Day 8](/blog/2026-05-31-no-build-web-dom-and-events) — the browser's primitives compose.

## Part 8: `requestSubmit()` — The Right Way To Submit Programmatically

For years, programmatically submitting a form was a mess:

- `form.submit()` bypasses validation entirely. Submits even if invalid. Almost never what you want.
- Triggering a click on the submit button works but requires a submit button to exist and be enabled.
- Synthesising a submit event with `dispatchEvent` does not actually trigger the form's default behaviour.

`form.requestSubmit()` does the right thing: runs validation, fires the `submit` event, and submits if and only if validation passes.

```javascript
button.addEventListener("click", () => {
  form.requestSubmit();   // fires submit event with full validation
});
```

You can pass a specific submitter:

```javascript
form.requestSubmit(saveDraftButton);   // submits as if saveDraftButton was clicked
```

The submit event includes the submitter via `event.submitter`, useful for forms with multiple submit buttons:

```html
<form>
  <button type="submit" name="action" value="save">Save</button>
  <button type="submit" name="action" value="publish">Publish</button>
</form>
```

```javascript
form.addEventListener("submit", (event) => {
  event.preventDefault();
  const action = event.submitter?.value;
  if (action === "save") saveDraft();
  else if (action === "publish") publish();
});
```

`requestSubmit()` is [Baseline since 2022](https://developer.mozilla.org/en-US/docs/Web/API/HTMLFormElement/requestSubmit). Use it. There is essentially no reason to use plain `form.submit()` ever again.

## Part 9: A Complete Worked Example — Sign-Up Form

Putting it all together. A complete sign-up form with every native feature wired up:

```html
<!doctype html>
<html lang="en">
<head>
  <meta charset="utf-8">
  <meta name="viewport" content="width=device-width, initial-scale=1">
  <title>Sign up</title>
  <link rel="stylesheet" href="/css/app.css">
  <style>
    .field { display: grid; gap: 0.25rem; margin-block: 1rem; }
    .field label { font-weight: 600; }
    .field input {
      padding: 0.5rem 0.75rem;
      border: 1px solid oklch(80% 0 0);
      border-radius: 0.375rem;
      font: inherit;
      transition: border-color 150ms;
    }
    .field input:focus-visible {
      outline: 2px solid oklch(55% 0.15 250);
      outline-offset: 2px;
    }
    .field input:user-invalid {
      border-color: oklch(55% 0.2 25);
    }
    .field input:user-valid {
      border-color: oklch(55% 0.15 145);
    }
    .field .hint {
      font-size: 0.875em;
      color: oklch(40% 0 0);
    }
    .field .error {
      font-size: 0.875em;
      color: oklch(55% 0.2 25);
      display: none;
    }
    .field input:user-invalid + .error {
      display: block;
    }
    .field .status[data-state="checking"] { color: oklch(50% 0 0); }
    .field .status[data-state="ok"]       { color: oklch(55% 0.15 145); }
    .field .status[data-state="error"]    { color: oklch(55% 0.2 25); }
    button[type="submit"] {
      padding: 0.625rem 1.25rem;
      background: oklch(55% 0.15 250);
      color: white;
      border: none;
      border-radius: 0.375rem;
      font: inherit;
      cursor: pointer;
    }
    form:has(:user-invalid) button[type="submit"] {
      opacity: 0.6;
    }
  </style>
</head>
<body>
  <h1>Create your account</h1>

  <form id="signup" novalidate>
    <div class="field">
      <label for="name">Name</label>
      <input id="name" type="text" name="name"
             autocomplete="name" required minlength="2"
             aria-describedby="name-error">
      <span id="name-error" class="error">Please enter your name.</span>
    </div>

    <div class="field">
      <label for="email">Email</label>
      <input id="email" type="email" name="email"
             autocomplete="email" required
             aria-describedby="email-error">
      <span id="email-error" class="error">Please enter a valid email.</span>
    </div>

    <div class="field">
      <label for="username">Username</label>
      <input id="username" type="text" name="username"
             autocomplete="username" required
             minlength="3" maxlength="20"
             pattern="[a-zA-Z0-9_]+"
             aria-describedby="username-hint username-error">
      <span id="username-hint" class="hint">
        3–20 characters: letters, numbers, or underscores.
      </span>
      <span id="username-error" class="error">
        That username is not available.
      </span>
      <span class="status" aria-live="polite"></span>
    </div>

    <div class="field">
      <label for="password">Password</label>
      <input id="password" type="password" name="password"
             autocomplete="new-password" required minlength="12"
             aria-describedby="password-hint">
      <span id="password-hint" class="hint">
        At least 12 characters. We recommend a password manager.
      </span>
    </div>

    <div class="field">
      <label for="dob">Date of birth</label>
      <input id="dob" type="date" name="dob"
             autocomplete="bday" required
             min="1900-01-01" max="2008-01-01">
    </div>

    <button type="submit">Create account</button>
  </form>

  <script type="module">
    import { debounce } from "/js/utils/dom.js";

    const form = document.getElementById("signup");
    const username = form.elements.username;
    const usernameStatus = username.parentElement.querySelector(".status");

    let abortController;

    const checkUsername = debounce(async () => {
      if (!username.value || !username.checkValidity()) {
        usernameStatus.textContent = "";
        usernameStatus.dataset.state = "";
        return;
      }
      abortController?.abort();
      abortController = new AbortController();
      usernameStatus.textContent = "Checking…";
      usernameStatus.dataset.state = "checking";
      try {
        const response = await fetch(
          `/api/check-username?u=${encodeURIComponent(username.value)}`,
          { signal: abortController.signal }
        );
        if (abortController.signal.aborted) return;
        const { taken } = await response.json();
        if (taken) {
          username.setCustomValidity("That username is taken.");
          usernameStatus.textContent = "Already taken";
          usernameStatus.dataset.state = "error";
        } else {
          username.setCustomValidity("");
          usernameStatus.textContent = "Available";
          usernameStatus.dataset.state = "ok";
        }
      } catch (e) {
        if (e.name !== "AbortError") {
          username.setCustomValidity("");
          usernameStatus.textContent = "";
          usernameStatus.dataset.state = "";
        }
      }
    }, 400);

    username.addEventListener("input", () => {
      // Clear any stale custom error so native validation can re-evaluate
      username.setCustomValidity("");
      checkUsername();
    });

    form.addEventListener("submit", async (event) => {
      event.preventDefault();
      // Wait for any in-flight async check
      if (usernameStatus.dataset.state === "checking") {
        usernameStatus.textContent = "Still checking — please wait";
        return;
      }
      if (!form.reportValidity()) return;

      // All checks passed: send the request
      const data = new FormData(form);
      try {
        const response = await fetch("/api/signup", {
          method: "POST",
          body: data,
        });
        if (!response.ok) throw new Error("Signup failed");
        location.href = "/welcome";
      } catch (error) {
        alert("Could not create account: " + error.message);
      }
    });
  </script>
</body>
</html>
```

Going through the highlights:

- **Every input has the right `type`, `inputmode`, and `autocomplete`.** Returning users on mobile fill this form in seconds. New users get the right keyboard for each field.
- **Constraints are declared in HTML** — `required`, `minlength`, `maxlength`, `pattern`, `min`, `max`. The browser validates without any JavaScript.
- **`novalidate` on the form** suppresses the browser's native popup, because we handle styling ourselves. Validation still *runs* (`reportValidity` triggers it); we just present the results our way.
- **`:user-invalid`** drives the error styling — fields go red only after the user interacts.
- **Custom error messages** appear via `+ .error { display: block }` when the field is `:user-invalid`. Tied to inputs via `aria-describedby` for screen readers.
- **`aria-live="polite"`** on the status element announces async-validation state changes.
- **Async username check** uses `setCustomValidity` to integrate with the native validation system. Debounced. AbortController for race-free results. Network errors do not block submit.
- **Submit handler** waits for any in-flight check, runs `reportValidity`, and only submits if everything passes.
- **`form:has(:user-invalid) button[type="submit"]`** visually fades the submit button when the form has errors.

Total: about 80 lines of HTML, 30 lines of CSS, 50 lines of JavaScript. Fully accessible. Validates on the client, validates on the server, handles network errors, debounces, cancels stale requests. No form library. No validation schema library. No state machine library.

If you want to compare: an equivalent `Formik + Yup + axios` implementation would be approximately 250–400 lines of TypeScript, plus 80KB of dependencies, and would still need to add the `inputmode` and `autocomplete` attributes manually because libraries do not add them for you.

## Part 10: Form-Associated Custom Elements In Forms

We covered Form-Associated Custom Elements in [Day 10](/blog/2026-06-02-no-build-web-custom-elements). They participate fully in the Constraint Validation API. A `<rating-input>` with `setValidity({ valueMissing: true }, "Please pick a rating.")` is part of the same `form:has(:user-invalid)` selectors, the same `form.reportValidity()`, the same `FormData` capture.

```html
<form id="review" novalidate>
  <label>Rating
    <rating-input name="rating" required max="5"></rating-input>
  </label>
  <textarea name="comment" required minlength="10"
            aria-describedby="comment-error"></textarea>
  <span id="comment-error" class="error">Please write at least 10 characters.</span>
  <button type="submit">Submit review</button>
</form>
```

The `<rating-input>` participates in the form like any native control. `form.reportValidity()` validates both the rating and the comment. `FormData` captures the rating value. Custom-element forms compose seamlessly with native forms.

## Part 11: Honest Limits Of Native Forms

A complete picture requires honesty about where native forms struggle.

**1. Cross-field error summaries.** "Five errors on this page; click here to fix them" UI requires JavaScript — there is no native widget for it. Easy to build (~20 lines using `form.querySelectorAll(":invalid")`).

**2. Fancy in-page validation animations.** "The error message slides in" effects need JavaScript or `@starting-style` (Day 6).

**3. Multi-step forms / wizards.** Native forms are single-page. Multi-step requires JavaScript to manage state across "pages" of the form. Use the patterns from Day 9 (state, URL params).

**4. Conditional fields.** "Show this field only if the user picks 'other'" can be done with `:has()` + CSS (Day 5), but for complex dependencies (show field B if A is X, hide field C if A is X but only when D is Y) JavaScript is clearer.

**5. Localised validation messages.** The browser's default messages are localised to the user's browser locale. If you set messages with `setCustomValidity`, you supply them — usually in the page's language. For multi-locale sites this is more code.

**6. `<input type="file">` UI customisation.** The native UI is hard to style consistently. Workarounds use a hidden file input behind a styled label — fine, but a wart.

**7. `<select>` styling.** Until [`appearance: base`](https://developer.chrome.com/blog/styling-form-controls) reaches Widely Available, `<select>` is hard to style consistently. Custom `<select>` widgets are notoriously hard to make accessible. The pragmatic answer: live with the native style, or use a battle-tested library only for `<select>`.

For everything else — basic to moderately-complex forms — the platform is enough.

## Part 12: When You Still Want A Form Library

In our consulting experience, the most common cases:

1. **Forms with deeply branching field dependencies** (insurance applications, tax forms, compliance forms). A library that explicitly models conditional logic is genuinely useful.
2. **Schema-driven forms** where the field set is generated from a backend schema (JSON Schema, OpenAPI). React JSON Schema Form, FormKit, JSON-forms. Generating native HTML manually for hundreds of fields is tedious.
3. **Multi-step wizards with complex state.** TanStack Form, React Hook Form's wizard patterns. Native form + JS state can do it (we showed how in Day 9), but a library may be cleaner past a certain complexity.
4. **Existing React/Vue codebases with strong opinions about form state.** Code costs money to change. If `Formik` is working for you, do not migrate.

For simple to moderate forms — the sign-up form above, contact forms, comment composers, search filters, settings panels — the platform is faster, smaller, more accessible, and easier to maintain. Reach for a library only when the form genuinely demands it.

## Part 13: Tomorrow

Tomorrow — **Day 13: Storage, Service Workers, and Offline** — we cover the persistence and offline story. `localStorage` (small, sync, simple), `IndexedDB` (bigger, async, capable), Cache API and Service Workers for offline-first apps, Background Sync for offline form submissions, the manifest file that makes a site installable as a PWA. We will turn our magazine into a fully-installable, offline-first app — the same site, no extra build, just a few hundred extra lines of mostly-declarative configuration.

See you tomorrow.

---

## Series navigation

You are reading **Part 12 of 15**.

- [Part 1: Overview — Why a Plain Browser Is Enough in 2026](/blog/2026-05-24-no-build-web-overview)
- [Part 2: Semantic HTML and the Document Outline Nobody Taught You](/blog/2026-05-25-no-build-web-html-semantics)
- [Part 3: The Cascade, Specificity, and `@layer`](/blog/2026-05-26-no-build-web-css-cascade-layers)
- [Part 4: Modern CSS Layout — Flexbox, Grid, Subgrid, and Container Queries](/blog/2026-05-27-no-build-web-modern-css-layout)
- [Part 5: Responsive Design in 2026](/blog/2026-05-28-no-build-web-responsive-design)
- [Part 6: Colour, Typography, and Motion](/blog/2026-05-29-no-build-web-color-typography-motion)
- [Part 7: Native ES Modules](/blog/2026-05-30-no-build-web-es-modules)
- [Part 8: The DOM, Events, and Platform Primitives](/blog/2026-05-31-no-build-web-dom-and-events)
- [Part 9: State Management Without a Library](/blog/2026-06-01-no-build-web-state-and-signals)
- [Part 10: Web Components](/blog/2026-06-02-no-build-web-custom-elements)
- [Part 11: Client-Side Routing with the Navigation API and View Transitions](/blog/2026-06-03-no-build-web-routing-and-navigation)
- **Part 12 (today): Forms, Validation, and the Constraint Validation API**
- Part 13 (tomorrow): Storage, Service Workers, and Offline
- Part 14: Accessibility, Performance, and Security
- Part 15: The Conclusion — A Complete Application End to End

## Resources

- [MDN: Constraint Validation API](https://developer.mozilla.org/en-US/docs/Web/API/Constraint_validation) — the canonical reference.
- [MDN: ValidityState](https://developer.mozilla.org/en-US/docs/Web/API/ValidityState) — every flag explained.
- [MDN: HTMLInputElement](https://developer.mozilla.org/en-US/docs/Web/API/HTMLInputElement) — the input element's full interface.
- [MDN: `:user-valid` and `:user-invalid`](https://developer.mozilla.org/en-US/docs/Web/CSS/:user-valid) — the modern validation pseudo-classes.
- [MDN: `requestSubmit()`](https://developer.mozilla.org/en-US/docs/Web/API/HTMLFormElement/requestSubmit) — the right way to submit programmatically.
- [WHATWG: autocomplete attribute reference](https://html.spec.whatwg.org/multipage/form-control-infrastructure.html#autofill) — every token, with definitions.
- [web.dev: User-valid and user-invalid pseudo-classes](https://web.dev/articles/user-valid-and-user-invalid-pseudo-classes) — the introduction.
- [web.dev: HTML form features you might not know about](https://web.dev/articles/forms-html-features-might-not-know) — comprehensive walkthrough.
- [Adam Silver, *Form Design Patterns*](https://www.smashingmagazine.com/printed-books/form-design-patterns/) — the canonical book on form UX.
- [NIST Special Publication 800-63B](https://pages.nist.gov/800-63-3/sp800-63b.html) — the modern password guidelines.
- [Caniuse: `:user-invalid`](https://caniuse.com/css-pseudo-user-invalid) — current browser support.
- [Stephanie Eckles, *Modern CSS for Dynamic Component-Driven Forms*](https://moderncss.dev/) — practical patterns.

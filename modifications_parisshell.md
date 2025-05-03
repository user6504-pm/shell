# Modifications — ParisShell (Post-Rendu 3)

This document summarizes the updates and new features introduced after the third submission of the ParisShell project.

---

## General Fixes & Improvements

- **Whitespace Handling:** Automatic removal of spaces in SIRET and phone number inputs.
- **Command Restrictions:** `changerole` command is no longer available for company-type users.
- **Login Logic:** Prevents calling `login` if a user is already authenticated.
- **Edit Behavior:** Auto-refresh after running the `edit client` command.
- **Display Correction:** Removed duplicate `showtable` call in the client module.
- **Order Cancellation UX:** Added a confirmation message before canceling an order via `client neworder`.
- **Cancel Command Check:** `client cancel` is now blocked if no active order exists for the user.

---

## Major New Features


### Email Confirmation System

- Upon creating a new account via the `register` command, an automatic email is sent to the user.
- The email contains a unique verification code required to confirm the registration.
- This ensures that the email address is valid and adds a security layer to the user registration process.

---

### Command Rating System

- Clients can **rate individual orders** via the new command: `client assess`.
- Cooks can **view their ratings** using: `cook stats`.
- Ratings are **visible to clients** at the time of placing a new order.
- This introduces a **reputation layer** for cooks and helps guide future decisions.

---

## Technical Fixes

- `verifycommands`: Logic has been revised and corrected.
- `cook commands`: Updated to reflect proper role logic and access controls.

---

## Notes

- The rating system represents a significant extension to the platform.
- All changes are tested and documented, and major behaviors have been reviewed.
- Minor refinements are still under consideration for future iterations.

---

For full functionality and user interaction details, please refer to the primary [README](README.md).

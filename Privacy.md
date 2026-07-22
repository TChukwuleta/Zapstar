# Zapstar Privacy Policy

Last updated: July 2026

Zapstar is a browser extension for sending Bitcoin Lightning tips to your favorite GitHub repos and contributors. Here's what it does and doesn't do with your data.

## What it does

- Reads public GitHub data — FUNDING.yml files, user bios, profile READMEs — to check if a repo or user has a Lightning Address
- Sends that public data to Zapstar's own API (api.zapstar.net) to verify the address is a lightning address and generate invoices
- Loads a QR code image from a third-party service (api.qrserver.com) so the invoice can be scanned
- Stores one setting locally in your browser — an optional API URL override, only used for local development

## What it doesn't do

- Doesn't collect or store any personal information about you
- Doesn't track what you browse or how you use it
- Never holds your funds — payments go straight from your wallet to the recipient's wallet
- No cookies, no analytics, no ad tracking
- Doesn't sell or share data with anyone

## Third parties involved

- GitHub — for reading public repo and profile data
- api.qrserver.com — for rendering the invoice QR code
- Whatever Lightning wallet or BTCPay instance the recipient's address points to — for resolving addresses and generating invoices

## Questions

Open an issue on the repo: https://github.com/TChukwuleta/Zapstar
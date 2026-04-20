# DevToys.Base64Url
[![NuGet]()]()

An extension for [DevToys](https://devtoys.app) which adds Base64Url support.

## Features

- **Encode** plain text to Base64Url format (RFC 4648 – URL-safe alphabet, no padding)
- **Decode** Base64Url strings back to plain text
- Supports **UTF-8** and **ASCII** encodings
- **Multiline mode** – encode or decode each line independently
- **Smart detection** – automatically recognises Base64Url content pasted from the clipboard

## Installation

1. Download the `DevToys.Base64Url` package from [NuGet.org]().
2. In DevToys, open **Manage Extensions**, click **Install an extension**, and select the downloaded package.

## What is Base64Url?

Base64Url is a URL-safe variant of Base64 (defined in [RFC 4648](https://datatracker.ietf.org/doc/html/rfc4648#section-5)):

| Standard Base64 | Base64Url   |
|-----------------|-------------|
| `+`             | `-`         |
| `/`             | `_`         |
| `=` (padding)   | *(omitted)* |


<p align="center">
  <img src="./assets/banner.png" alt="SimplyDraft" width="500">
</p>

<p align="center">
  <sub><b>v1.0.2</b></sub>
</p>

---

SimplyDraft is a desktop software that generate documents from reusable templates that you define.

* Define your template once, then produce consistent documents
* Supports Python-style scripting embedded in the software for text substitution
* Supports LaTeX-style markups for document formatting
* Currently supported outputs: .docx and .txt

<br>

<img width="1917" height="987" alt="image" src="https://github.com/user-attachments/assets/7fdff65c-b964-4819-b9ec-dfc44f350e9f" />

## Use Cases

Define once. Generate many.

Create reusable document templates for standardized or repetitive documentation, then generate project-specific variants from the same template.

* Ideal for documents that follow a consistent structure, such as IOQ documents, commissioning reports, FAT/SAT protocols, validation documents, and transcripts.
* Keep formatting, structure, and boilerplate content standardized while changing only the information that varies between projects.
* Instead of copying an old document and editing it manually, maintain one source template and generate each required version from it.

## Workflow

This is the Editor window:

<img width="850" height="485" alt="image" src="https://github.com/user-attachments/assets/e52b7c02-af24-4587-a32e-d7d433e67bec" />

* The top left pane is the Editor pane, where you can do edits.
* The bottom left pane is the Preview pane, which simulates what the document may look like after you export to docx or txt.
* The top right pane is the Scripting pane. Here, you can write Python-style scripting to substitute in the variable names.
* The middle right pane shows the Variable pane, which displays all variables currently used in the editor pane (auto-generated each time you encapsulate a text in { } brackets)
* The bottom right pane is the Diagnostic pane, and shows warnings or syntax errors in the scripting pane or in the editor pane

<br>

You can drag the other panes to minimize them. For example,

<img width="850" height="485" alt="image" src="https://github.com/user-attachments/assets/f4c84183-37ee-4a67-8b35-b28170117e2d" />

<br><br>

For the Python-like scripting, or LaTeX-like markups, if you need in-app help, simply click the '?' button which would show:

<img width="500" height="426" alt="image" src="https://github.com/user-attachments/assets/0a8cf19c-b0c1-44f7-ba51-110b321ecbda" />
<img width="500" height="426" alt="image" src="https://github.com/user-attachments/assets/48b5e1ca-2e64-45ba-b667-b77a5fb2eaf3" />

<br><br>

Once you have configured your templates in the Editor Window, you can generate child documents:

<img width="1400" height="877" alt="image" src="https://github.com/user-attachments/assets/4f406cca-0e8c-4e9b-a802-2a7bb9ec47db" />

<br><br>

Once done, check the Diagnostic pane and Preview pane. If all looks good, click the MenuHeader File -> Export to .txt or .docx

<img width="1436" height="947" alt="image" src="https://github.com/user-attachments/assets/fe82c667-7aed-4309-a798-d11245bf317d" />

## Licensing

Copyright (c) 2026 Tan Jing Ming

This project is licensed under the **PolyForm Strict License 1.0.0**.

You may use this software for permitted non-commercial purposes. Redistribution, modification, and creation of derivative works are not permitted.

See [LICENSE](./LICENSE.md) for the full terms.

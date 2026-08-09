<p align="center">
  <img src="./assets/banner.png" alt="SimplyDraft" width="500">
</p>

---

SimplyDraft is a desktop software that generate documents from reusable templates that you define.

* Define your template once, then produce consistent documents
* Supports Python-style scripting embedded in the software for text substitution
* Supports LaTeX-style markups for document formatting
* Currently supported outputs: .docx and .txt

## Use Cases

Define once. Generate many.

Create reusable document templates for standardized or repetitive documentation, then generate project-specific variants from the same template.

* Ideal for documents that follow a consistent structure, such as IOQ documents, commissioning reports, FAT/SAT protocols, validation documents, and transcripts.
* Keep formatting, structure, and boilerplate content standardized while changing only the information that varies between projects.
* Instead of copying an old document and editing it manually, maintain one source template and generate each required version from it.

## Workflow

**1. Create a new template**

You can choose an existing template shipped with the application, or create your own:

<img width="1231" height="757" alt="image" src="https://github.com/user-attachments/assets/0765e9ff-ba49-4245-8034-649a3b6c6c4f" />

<br><br>

**2. Edit your template**

This is the Editor window:

<img width="1917" height="985" alt="image" src="https://github.com/user-attachments/assets/e52b7c02-af24-4587-a32e-d7d433e67bec" />

* The top left pane is the Editor pane, where you can do edits.
* The bottom left pane is the Preview pane, which simulates what the document may look like after you export to docx or txt.
* The top right pane is the Scripting pane. Here, you can write Python-style scripting to substitute in the variable names.
* The middle right pane shows the Variable pane, which displays all variables currently used in the editor pane (auto-generated each time you encapsulate a text in { } brackets)
* The bottom right pane is the Diagnostic pane, and shows warnings or syntax errors in the scripting pane or in the editor pane

<br><br>

You can minimize the other panes to make it simpler. For example, simply drag the panes to minimize them:

<img width="1917" height="985" alt="image" src="https://github.com/user-attachments/assets/f4c84183-37ee-4a67-8b35-b28170117e2d" />

<br>

For the Python-like scripting, or LaTeX-like markups, if you need in-app help, simply click the '?' button which would show:
<img width="500" height="422" alt="image" src="https://github.com/user-attachments/assets/5e04b939-a193-46a6-9059-7e1a926e327d" />
<img width="500" height="426" alt="image" src="https://github.com/user-attachments/assets/48b5e1ca-2e64-45ba-b667-b77a5fb2eaf3" />

<br>

**3. Template to Child**

Once you have configured your templates in the Editor Window, return to the Main Window, and click on Children tab.

Right click the workspace and click 'New Child (generate)...', which would show a new dialog:
<img width="517" height="502" alt="image" src="https://github.com/user-attachments/assets/55922f38-bf4c-4f05-9586-2815e874b8a0" />

<br><br>

Select the template you want, give the child document a name, and then substitute in the variable values:
<img width="1315" height="877" alt="image" src="https://github.com/user-attachments/assets/4f406cca-0e8c-4e9b-a802-2a7bb9ec47db" />

<br>

Once done, check the Diagnostic pane and Preview pane. If all looks good, click the MenuHeader File -> Export to .txt or .docx
<img width="1436" height="947" alt="image" src="https://github.com/user-attachments/assets/fe82c667-7aed-4309-a798-d11245bf317d" />

## Licensing

Copyright (c) 2026 Tan Jing Ming

This project is licensed under the **PolyForm Noncommercial License 1.0.0**.

You may use this software for permitted non-commercial purposes. Redistribution, modification, and creation of derivative works are not permitted.

See [LICENSE](LICENSE) for the full terms.

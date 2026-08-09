# SimplyDraft
C# Avalonia desktop application to ease document creation.

Template-based document generation- write a template once, and then use embedded scripting (Python-style) or markups (LaTeX-style) to generate documents.
-> Currently supported outputs: docx and txt

Use cases:
1. Easier generation of repetitive documents, such as document templates for different projects or clients, or video recording transcripts


## Workflow
**1. Create a new template**
<img width="1231" height="757" alt="image" src="https://github.com/user-attachments/assets/0765e9ff-ba49-4245-8034-649a3b6c6c4f" />
- You can create a new template, or use the existing templates shipped with the app (.exe).


**2. Edit your template**
<img width="1917" height="985" alt="image" src="https://github.com/user-attachments/assets/e52b7c02-af24-4587-a32e-d7d433e67bec" />

This is the Editor Window.
- The top left pane is the Editor pane, where you can do edits.
- The bottom left pane is the Preview pane, which simulates what the document may look like after you export to docx or txt.
- The top right pane is the Scripting pane. Here, you can write Python-style scripting to substitute in the variable names.
- The middle right pane shows the Variable pane, which displays all variables currently used in the editor pane (auto-generated each time you encapsulate a text in { } brackets)
- The bottom right pane is the Diagnostic pane, and shows warnings or syntax errors in the scripting pane or in the editor pane


You can minimize the other panes to make it simpler.
For example, simply drag the panes to minimize them:
<img width="1917" height="985" alt="image" src="https://github.com/user-attachments/assets/f4c84183-37ee-4a67-8b35-b28170117e2d" />


For the Python-like scripting, or LaTeX-like markups, if you need in-app help, simply click the '?' button which would show:
<img width="500" height="422" alt="image" src="https://github.com/user-attachments/assets/5e04b939-a193-46a6-9059-7e1a926e327d" />
<img width="500" height="426" alt="image" src="https://github.com/user-attachments/assets/48b5e1ca-2e64-45ba-b667-b77a5fb2eaf3" />


**3. Template to Child**
Once you have configured your templates in the Editor Window, return to the Main Window, and click on Children tab.

Right click the workspace and click 'New Child (generate)...'

This would show a new dialog:

<img width="517" height="502" alt="image" src="https://github.com/user-attachments/assets/55922f38-bf4c-4f05-9586-2815e874b8a0" />

Select the template you want, give the child document a name, and then substitute in the variable values:

<img width="1315" height="877" alt="image" src="https://github.com/user-attachments/assets/4f406cca-0e8c-4e9b-a802-2a7bb9ec47db" />

Once done, check the Diagnostic pane and Preview pane. If all looks good, click the MenuHeader File -> Export to .txt or .docx

<img width="1436" height="947" alt="image" src="https://github.com/user-attachments/assets/fe82c667-7aed-4309-a798-d11245bf317d" />


## Licensing
Copyright (c) 2026 Tan Jing Ming

This project is licensed under the **PolyForm Noncommercial License 1.0.0**.

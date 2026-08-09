<img width="1231" height="757" alt="image" src="https://github.com/user-attachments/assets/3c977e51-4a4b-4623-a64f-091257daf29a" /># SimplyDraft
C# Avalonia desktop application to ease document creation.

Template-based document generation- write a template once, and then use embedded scripting (Python-style) or markups (LaTeX-style) to generate documents.
-> Currently supported outputs: docx and txt

Use cases:
1. Easier generation of repetitive documents, such as document templates for different projects or clients, or video recording transcripts

<img width="1438" height="913" alt="image" src="https://github.com/user-attachments/assets/5a71e030-737e-4a4f-b66e-d40b30c1cce4" />

## Workflow
1. Create a new template, or use the existing seeded templates bundled with the App
<img width="1231" height="757" alt="image" src="https://github.com/user-attachments/assets/0765e9ff-ba49-4245-8034-649a3b6c6c4f" />

2. In the Editor Window:
<img width="1917" height="985" alt="image" src="https://github.com/user-attachments/assets/e52b7c02-af24-4587-a32e-d7d433e67bec" />

- The top left pane is the editor pane.
- The bottom left pane is the preview pane (what will be generated after exporting).
- The top right pane is the scripting pane. You can write in scripts in Python-style for the variables
- The middle right pane shows all variables currently used in the editor pane (auto-generated each time you encapsulate a text in { } brackets)
- The bottom right pane is the diagnostic pane, and shows warnings or syntax errors in the scripting pane or in the editor pane

You can minimize the other panes to make it simpler if you do not want to use scripting.
For example, simply drag the panes to minimize them:
<img width="1917" height="985" alt="image" src="https://github.com/user-attachments/assets/f4c84183-37ee-4a67-8b35-b28170117e2d" />




## Licensing
Copyright (c) 2026 Tan Jing Ming

This project is licensed under the **PolyForm Noncommercial License 1.0.0**.

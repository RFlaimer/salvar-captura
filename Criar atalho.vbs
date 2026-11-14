Option Explicit
Dim files, shell, folder, link
Set files = CreateObject("Scripting.FileSystemObject")
Set shell = CreateObject("WScript.Shell")
folder = files.GetParentFolderName(WScript.ScriptFullName)
If Not files.FileExists(files.BuildPath(folder, "SalvarCaptura.exe")) Then
    MsgBox "Extraia o ZIP e mantenha este arquivo ao lado de SalvarCaptura.exe.", 48, "Salvar Captura"
    WScript.Quit 1
End If
Set link = shell.CreateShortcut(shell.SpecialFolders("Desktop") & "\Salvar Captura.lnk")
link.TargetPath = files.BuildPath(folder, "SalvarCaptura.exe")
link.WorkingDirectory = folder
link.IconLocation = link.TargetPath & ",0"
link.Description = "Cole sua print, recorte e salve no formato desejado."
link.WindowStyle = 1
link.Save
MsgBox "Atalho Salvar Captura criado na area de trabalho.", 64, "Salvar Captura"

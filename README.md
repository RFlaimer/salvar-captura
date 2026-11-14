# Salvar Captura

<img src="assets/SalvarCaptura.png" alt="Ícone Salvar Captura" width="96">

Criei esta ferramenta para facilitar a forma de salvar sua print, sem precisar abrir um programa ou aplicativo de edição de imagens separado. Basta tirar a captura e pressionar **Ctrl+Alt+S**: a ferramenta abre, cola a imagem e mostra a janela para salvar em JPEG, na pasta Imagens. Se precisar recortar ou redimensionar, clique em **Cancelar** na janela de salvamento para editar a captura no aplicativo.

## Download para Windows

[**Baixar a versão mais recente**](https://github.com/RFlaimer/salvar-captura/releases/latest)

1. Baixe o arquivo **SalvarCaptura-v1.1.0-Windows.zip** em Releases.
2. Extraia o ZIP para uma pasta que você queira manter.
3. Abra **SalvarCaptura.exe**. Não é necessário instalar.
4. Execute **Criar atalho.vbs** para adicionar um atalho com ícone na área de trabalho e habilitar **Ctrl+Alt+S**.

## Como usar

- Faça a captura com **Print Screen** e conclua a seleção do Windows.
- Pressione **Ctrl+Alt+S** ou abra o atalho: se houver uma imagem na área de transferência, ela será colada automaticamente e a janela **Salvar como** será aberta.
- Salve diretamente ou clique em **Cancelar** para voltar à prévia e editar a captura.
- Para colar outra captura com a janela aberta, use **Ctrl+V**.
- **Recortar:** selecione com o mouse a área que deseja manter e aplique.
- **Redimensionar:** informe largura ou altura em pixels; a opção de manter proporção vem ativada.
- **Restaurar:** volte à imagem original da última colagem.
- **Transcrever texto:** extraia o texto da imagem localmente, confira o resultado e use **Copiar texto**. O botão usa o ícone de documentos fornecido pelo autor.
- Pressione **Ctrl+S** para escolher o nome e salvar.

Abrir `SalvarCaptura.exe` diretamente também cola a imagem disponível, mas deixa a prévia aberta para edição. O atalho usa o argumento `--salvar` para abrir o salvamento imediato. **Se a área de transferência estiver vazia ou contiver texto ou arquivos, pressionar Ctrl+Alt+S não abre nenhuma janela.** Conteúdos mistos com texto ou arquivos também são ignorados, mesmo quando incluem uma representação em bitmap.

A janela de salvamento abre sempre em **Imagens**, com **JPEG (.jpg)** como padrão. Também estão disponíveis PNG, BMP, GIF e TIFF. A prévia se ajusta à janela; o arquivo usa as dimensões indicadas em pixels.

O atalho de teclado é gerenciado pelo Windows. A ferramenta não fica em segundo plano, não monitora sua área de transferência e não inicia junto com o sistema. Fechar a janela encerra o aplicativo. Mantenha o atalho na área de trabalho para usar **Ctrl+Alt+S**. Se outra ferramenta já usar essa combinação, pode ocorrer conflito.

As imagens são processadas localmente. O aplicativo não envia capturas pela internet. Se você salvar em uma pasta sincronizada, como o OneDrive, a sincronização é feita pelo serviço instalado no seu computador.

O OCR utiliza o reconhecimento de texto do Windows e os idiomas disponíveis no dispositivo, com preferência para português quando instalado. Quando houver mais de um idioma instalado, um seletor permite escolher o idioma do texto antes da transcrição. Ele transcreve a imagem atual, incluindo os recortes aplicados. Revise o resultado antes de usar: letras pequenas, imagens desfocadas e manuscritos podem não ser reconhecidos corretamente. Se não houver idioma de OCR instalado, a ferramenta informa isso; as outras funções continuam disponíveis.

## Requisitos

Windows 10 ou 11 com .NET Framework 4.x e interface gráfica. A ferramenta é portátil, não exige administrador e inclui ícone no executável e no atalho. O executável não possui assinatura digital.

JPEG utiliza compressão com perda; PNG é útil para textos e transparência. GIF tem paleta reduzida. JPEG e BMP usam fundo branco nas áreas transparentes.

Se a pasta escolhida não permitir gravação, a ferramenta oferece a subpasta **Capturas** ao lado do executável. Mantenha o aplicativo em uma pasta em que sua conta possa gravar.

## Compilar o código

No PowerShell, na pasta do projeto:

```powershell
powershell.exe -NoProfile -ExecutionPolicy Bypass -File .\build.ps1
```

O resultado fica em `dist/`. O script usa o compilador do .NET Framework presente no Windows e não baixa dependências.

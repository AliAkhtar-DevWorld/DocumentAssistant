# AI Lab: ASP.NET Core MVC, Ollama, embeddings, and RAG

This project demonstrates local AI features in an ASP.NET Core MVC application:

- **Ask Gemma:** send a question directly to the language model.
- **View Embedding:** see a numeric vector produced from text.
- **Compare Embeddings:** compare the similarity of text passages.
- **Ask with RAG:** find a relevant document passage and ask Gemma to answer using that passage.

The application uses **Gemma 3 4B** to generate answers and **EmbeddingGemma** to create embeddings. Ollama runs both models on your computer. The project currently demonstrates LLMs, embeddings, and RAG; it does not implement an AI agent.

## Requirements

1. **Windows, macOS, or Linux** capable of running the chosen local models. Model downloads require free disk space, and responses may be slower on less powerful computers.
2. **.NET 10 SDK.** Verify with `dotnet --version` in a terminal. Visual Studio 2026 with the **ASP.NET and web development** workload is an alternative way to build and run the project on Windows.
3. **Ollama**, installed from https://ollama.com/download . Install a current version that supports EmbeddingGemma.
4. Internet access for the initial SDK, package, and model downloads. Ollama serves the downloaded models locally after setup.

## 1. Download the models

Start the Ollama application, then run these commands in a terminal (PowerShell on Windows):

```powershell
ollama pull gemma3:4b
ollama pull embeddinggemma
ollama list
```

Check that `ollama list` shows both `gemma3:4b` and `embeddinggemma`. Keep Ollama running while you use the web app. Its local API is available at `http://localhost:11434`; the models are **not** stored in this repository.

If Ollama is not already running, start the application. On a system without the desktop app running, you can start its server in a separate terminal with:

```powershell
ollama serve
```

If `ollama serve` reports that port 11434 is already in use, Ollama may already be running. Check `http://localhost:11434/api/tags` in your browser or try `ollama list`.

## 2. Check the document used by RAG

The current `RagController` reads:

```text
DocumentAssistant.Web/Documents/LeavePolicy.txt
```

Make sure this file is included when you share the repository. You can put multiple paragraphs in it, with a **blank line between passages**. For example:

```text
Annual leave: Full-time employees receive 20 working days of annual leave per year.

Maternity leave: Eligible employees receive 12 weeks of maternity leave.
```

The filename is part of the current code. To use a different file, update the filename in `RagController`. This version reads plain text from that one file; other document formats would need their text extracted and supported in code first. The controller creates embeddings for the question and passages each time you ask a RAG question.

## 3. Run the MVC application

### Visual Studio 2026

1. Open the repository's solution file (`.sln` or `.slnx`). If there is no solution file, open `DocumentAssistant.Web/DocumentAssistant.Web.csproj`.
2. Set **DocumentAssistant.Web** as the startup project.
3. Restore packages if Visual Studio prompts you, then press **F5** or **Ctrl+F5**.
4. Open the address shown by Visual Studio and go to `/Chat` if it does not open there automatically.

### Command line

From the repository root, run:

```powershell
dotnet restore DocumentAssistant.Web/DocumentAssistant.Web.csproj
dotnet run --project DocumentAssistant.Web/DocumentAssistant.Web.csproj
```

Open the **Listening on** address printed in the terminal, then visit its `/Chat` page. The exact HTTP or HTTPS port can differ between machines and launch profiles; do not assume it will be 7210.

If HTTPS reports an untrusted development certificate, run `dotnet dev-certs https --trust` and restart the app.

## 4. Try the features

In **Ask Gemma**, enter `Explain embeddings in two sentences.` The app sends this question directly to `gemma3:4b`.

In **Ask with RAG**, enter `How many days of annual leave do employees receive?` when using the sample document above. The page should show the answer together with the retrieved source and passage. Also try a question that the file cannot answer and check whether the answer acknowledges that the source lacks the information.

## How the RAG request works

1. The MVC controller reads and splits the text in `LeavePolicy.txt`.
2. It sends the question and passages to Ollama's `/api/embed` endpoint using `embeddinggemma`.
3. C# compares the question vector to the passage vectors and selects the closest passage.
4. It sends the **question and original text of that passage** to `/api/generate` using `gemma3:4b`.
5. The browser displays Gemma's answer and the retrieved passage.

A high similarity score means a passage is related to the question. It does not guarantee that the passage contains the answer.

## Troubleshooting

| Problem | Check |
| --- | --- |
| Cannot connect to Ollama | Start Ollama and check `http://localhost:11434/api/tags`. The MVC application's named `Ollama` HTTP client must point to `http://localhost:11434`. |
| Model not found | Run `ollama list` and pull the exact names `gemma3:4b` and `embeddinggemma`. |
| `LeavePolicy.txt was not found` | Place the file under `DocumentAssistant.Web/Documents/` and confirm its name matches the controller. |
| First answer is slow | Wait for the local model to load. Local generation time depends on your machine. |
| Wrong or unsupported RAG answer | Read the *Retrieved source* on the page. The app selects one closest passage, which may not contain enough information to answer. |
| HTTPS certificate warning | Run `dotnet dev-certs https --trust`, then restart the web app. |

## Official downloads and references

- .NET: https://dotnet.microsoft.com/download
- Ollama installation: https://ollama.com/download
- Ollama Windows instructions: https://docs.ollama.com/windows
- Gemma model: https://ollama.com/library/gemma3:4b
- Embedding model: https://ollama.com/library/embeddinggemma
- Ollama embedding endpoint: https://docs.ollama.com/api/embed
- Ollama generation endpoint: https://docs.ollama.com/api/generate

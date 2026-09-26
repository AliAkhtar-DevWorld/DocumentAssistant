# Run DocumentAssistant

1. Install the **.NET 10 SDK** from https://dotnet.microsoft.com/download/dotnet/10.0 . On Windows, you can also use Visual Studio 2026 with the **ASP.NET and web development** workload.

2. Install **Ollama** from https://ollama.com/download and start the Ollama application.

3. Open PowerShell or a terminal and download the two models:

   ```text
   ollama pull gemma3:4b
   ollama pull embeddinggemma
   ```

4. Check that both models appear when you run `ollama list`. Keep Ollama running; the app connects to it at `http://localhost:11434`.

5. Clone or download this repository. Check that `DocumentAssistant/Documents/LeavePolicy.txt` exists; the RAG page needs it.

6. Open the solution in Visual Studio 2026 and Press **F5**.

7. Go to **`/Chat`**. The port number may vary between computers.

8. Try **Ask Gemma**, **View Embedding**, **Compare Embeddings**, and **Ask with RAG**. For RAG, ask a question answered by `LeavePolicy.txt`.

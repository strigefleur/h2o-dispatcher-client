using Felweed.Models;

namespace Felweed.Constants;

public static class EnvVariableConst
{
    public static List<EnvVariable> DefaultEnvVariables { get; set; } =
    [
        new("H2O_LLM_AUTO_COMMENTER_NAME", "gemma3:4b"),
        new("H2O_LLM_AUTO_COMMENTER_URL", "http://localhost:11434/v1/chat/completions"),
        new("H2O_LLM_AUTO_COMMENTER_TIMEOUT", "120"),
        new("H20_LLM_AUTO_COMMENTER_PROMPT", "Верни ТОЛЬКО исходный код с добавлением кратких комментариев " +
                                             "на русском языке; без markdown, без излишних пояснений; " +
                                             "используй summary для комментирования публичных элементов.")
    ];
}
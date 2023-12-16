using System.Transactions;
using Westwind.Utilities;

namespace Westwind.Ai.Test;

public class TestConfiguration
{
    static TestConfiguration()
    {
        Current =  JsonSerializationUtils.DeserializeFromFile<TestConfiguration>("__TestConfiguration-NoGit.json");
    }

    public static TestConfiguration Current { get; set; }

    public string OpenAiApiKey { get; set; }

    public string AzureConnectionString { get; set; }
    public string AzureApiKey { get; set; }


    
}
using Westwind.Utilities;

namespace Westwind.Ai.Test
{
    [TestClass]
    public class UnitTest1
    {
        public string OpenAiApiKey = "";

        public UnitTest1()
        {
            JsonSerializationUtils.DeserializeFromFile<("_TestConfiguration-NoGit.json");

            OpenAiApiKey = Environment.GetEnvironmentVariable("OPENAI_API_KEY");
        }

        [TestMethod]
        public void RawOpenAiImageGenerationTests()
        {



        }
    }
}
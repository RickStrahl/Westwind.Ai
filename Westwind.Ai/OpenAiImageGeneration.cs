using System.Net.Http.Headers;
using System.Text;
using Newtonsoft.Json;
using Westwind.Utilities;
using System.Text.RegularExpressions;

namespace Westwind.Ai;


/// <summary>
/// Open AI Image Generation
/// </summary>
public class OpenAiImageGeneration
{

    public string ApiKey { get; set; }

    public OpenAiImageGeneration(string apiKey)
    {
        ApiKey = apiKey;            
    }

    /// <summary>
    /// Generate an image from the provided prompt
    /// </summary>
    /// <param name="prompt">Prompt text for image to create</param>
    /// <param name="createImageFile">if true creates an image file and saves it into the OpanAiAddin\Images folder</param>
    /// <returns></returns>
    public async Task<bool> Generate(ImagePrompt prompt, bool createImageFile = false)
    {
        // structure for posting to the API
        var requiredImage = new ImageRequest()
        {
            prompt = prompt.Prompt?.Trim(),
            n = prompt.ImageCount,
            size = prompt.ImageSize,
            model = prompt.Model,
            style = prompt.ImageStyle,
            quality = prompt.ImageQuality
        };

        var imglink = new List<string>();
        var response = new ImageResults();

        using (var client = new HttpClient())
        {
            var json = JsonConvert.SerializeObject(requiredImage);
            client.DefaultRequestHeaders.Clear();
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", ApiKey);
            var message = await client.PostAsync("https://api.openai.com/v1/images/generations", new StringContent(json, Encoding.UTF8, "application/json"));
            if (message.IsSuccessStatusCode)
            {
                var content = await message.Content.ReadAsStringAsync();
                response = JsonConvert.DeserializeObject<ImageResults>(content);

                foreach (var url in response.data)
                {
                    imglink.Add(url.url);
                }
                prompt.ImageUrls = imglink.ToArray();
                prompt.RevisedPrompt = response.data[0].revised_prompt;
                if (prompt.Prompt == prompt.RevisedPrompt)
                    prompt.RevisedPrompt = null;

                if (createImageFile)
                {
                    try
                    {
                        await prompt.DownloadImageToFile();
                    }
                    catch (Exception ex)
                    {
                        SetError("Download failed: " + ex.Message);
                        return false;
                    }
                }
                return true;
            }
            else
            {
                if (message.StatusCode == System.Net.HttpStatusCode.BadRequest)
                {
                    if (message.Content.Headers.ContentLength > 0 && message.Content.Headers.ContentType?.ToString() == "application/json")
                    {
                        json = await message.Content.ReadAsStringAsync();
                        var error = JsonConvert.DeserializeObject<dynamic>(json);                            
                        string msg = error.error?.message;
                        //string code = error.error?.code;

                        SetError($"Image generation failed: {msg}");
                    }
                }

                return false;
            }
        }            
        //	return Json(response);
        //	curl https://api.openai.com/v1/images/generations \
        //  -H "Content-Type: application/json" \
        //  -H "Authorization: Bearer $OPENAI_API_KEY" \
        //  -d '{
        //
        //	"model": "dall-e-3",
        //    "prompt": "a white siamese cat",
        //    "n": 1,
        //    "size": "1024x1024"
        //  }

    }


    /// <summary>
    /// Currently doesn't work with Dall-E-3
    /// </summary>
    /// <param name="prompt"></param>
    /// <param name="createImageFile"></param>
    /// <returns></returns>
    public async Task<bool> CreateVariation(ImagePrompt prompt, bool createImageFile = false)
    {
        var imageFile = prompt.VariationImageFilePath;
        if (string.IsNullOrEmpty(imageFile) || !File.Exists(imageFile))
        {
            SetError("Input image file not found for variation.");
            return false;
        }

        var imglink = new List<string>();

        var ext = Path.GetExtension(imageFile).ToLower();
        var filename = Path.GetFileName(imageFile);

        using (var client = new HttpClient())
        {
            client.DefaultRequestHeaders.Clear();
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", ApiKey);

            var formContent = new MultipartFormDataContent();
            HttpResponseMessage message;
            using (var stream = File.OpenRead(imageFile))
            {
                var fileContent = new StreamContent(stream);
                fileContent.Headers.ContentType = new MediaTypeHeaderValue("image/png");
                //fileContent.Headers.ContentDisposition = new ContentDispositionHeaderValue("form-data");
                formContent.Add(fileContent, "image", filename);

                //formContent.Add(new StringContent(prompt.Model), "model");
                formContent.Add(new StringContent(prompt.ImageSize), "size");

                message = await client.PostAsync("https://api.openai.com/v1/images/variations", formContent);
            }

            if (message.IsSuccessStatusCode)
            {
                var content = await message.Content.ReadAsStringAsync();
                var response = JsonConvert.DeserializeObject<ImageResults>(content);

                foreach (var url in response.data)
                {
                    imglink.Add(url.url);
                    prompt.RevisedPrompt = url.revised_prompt;
                }                    
                prompt.ImageUrls = imglink.ToArray();

                if (createImageFile)
                {
                    try
                    {
                        await prompt.DownloadImageToFile();
                    }
                    catch (Exception ex)
                    {
                        SetError("Download failed: " + ex.Message);
                        return false;
                    }
                }
                return true;
            }

            if (message.StatusCode == System.Net.HttpStatusCode.BadRequest)
            {
                if (message.Content.Headers.ContentLength > 0 && message.Content.Headers.ContentType?.ToString() == "application/json")
                {
                    var json = await message.Content.ReadAsStringAsync();
                    var error = JsonConvert.DeserializeObject<dynamic>(json);
                    string msg = error.error?.message;
                    //string code = error.error?.code;

                    SetError($"Image generation failed: {msg}");
                }
            }

            return false;
        }

    }



    public async Task<byte[]> DownloadImage(string url)
    {
            
            

        return await HttpUtils.HttpRequestBytesAsync(url);
    }

        

    // ReSharper disable once ArrangeModifiersOrder
    public async static Task<bool> ValidateApiKeyAsync(string openAiKey)
    {

        var key = openAiKey;
        if (!Regex.IsMatch(key, @"^sk-[a-zA-Z0-9]{32,}$"))
            return false;

        using var client = new HttpClient();
        client.Timeout = TimeSpan.FromSeconds(3);
        client.DefaultRequestHeaders.Clear();
                        
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer",key);

        HttpResponseMessage response;
        try
        {                
            response = await client.GetAsync("https://api.openai.com/v1/models");
        }
        catch 
        {
            return false;
        }
        return response.IsSuccessStatusCode;
    }


    #region Error

    public string ErrorMessage { get; set; }

    protected void SetError()
    {
        SetError("CLEAR");
    }

    protected void SetError(string message)
    {
        if (message == null || message == "CLEAR")
        {
            ErrorMessage = string.Empty;
            return;
        }
        ErrorMessage += message;
    }

    protected void SetError(Exception ex, bool checkInner = false)
    {
        if (ex == null)
        {
            ErrorMessage = string.Empty;
        }
        else
        {
            Exception e = ex;
            if (checkInner)
                e = e.GetBaseException();

            ErrorMessage = e.Message;
        }
    }

    #endregion
}

/// <summary>
/// Open AI Image Request object in the format the API expects
/// </summary>
public class ImageRequest
{
    public string prompt { get; set; }

    public string model { get; set; } = "dall-e-3";

    public int n { get; set; } = 1;
    public string size { get; set; } = "1024x1024";

    public string response_format { get; set; } = "url";  // b64_json

    public string style { get; set; } = "vivid";  // natural
    public string quality { get; set; } = "standard";
}

public class ImageUrls
{
    public string url { get; set; }

    public string revised_prompt { get; set; }
}

public class ImageResults
{
    public long created { get; set; }
    public List<ImageUrls> data { get; set; }
        
}
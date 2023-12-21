# Westwind.Ai OpenAI Image Generation Sample

This is the code sample library for the article at:

 * [Integrating OpenAI image generation into a .NET Application](https://weblog.west-wind.com/posts/2023/Dec/21/Integrating-OpenAI-image-generation-into-your-NET-Application)
 
This library uses the OpenAI API via REST calls to retrieve image prompts as images and provide a number of support features for downloading and storing image prompts and images for later review and possible re-use. 

## How it works
The library is easy to use:

### Download to URL
You can generate images and let the API return a url to the image which you can then use to display the image, or can be used to download the image for local storage and later review.

> Note: Images are stored for a short time (a few hours) only, so if you want to reuse the images, you need to download them.


```csharp
[TestMethod]
public async Task ImageGenerationToUrlTest()
{
    var generator = new OpenAiImageGeneration(OpenAiApiKey);

    // create a prompt with input options
    var imagePrompt = new ImagePrompt()
    {
        Prompt = "A bear holding on to a snowy mountain peak, waving a beer glass in the air. Poster style, with a black background in goldenrod line art",
        ImageSize = "1024x1024",
        ImageQuality = "standard",
        ImageStyle = "vivid"
    };

    // Generate and set properties on `imagePrompt` instance
    Assert.IsTrue(await generator.Generate(imagePrompt), generator.ErrorMessage);

    // prompt returns an array of images, but for Dall-e-3 it's always one
    // so FirstImage returns the first image.
    var imageUrl = imagePrompt.FirstImageUrl;
    Console.WriteLine(imageUrl);

    // Open in default image viewer
    ShellUtils.GoUrl(imageUrl);

    // Typically the AI **fixes up the prompt**
    Console.WriteLine(imagePrompt.RevisedPrompt);

    // Optionally you can download the image from the captured URL to a local file
    // Default folder is %temp%\openai-images\images or specify `ImageFolderPath`
    // or specify: imagePrompt.ImageFolderPath = "c:\\temp\\openai-images\\"; 
    Assert.IsTrue(await imagePrompt.DownloadImageToFile(), "Image saving failed: " + generator.ErrorMessage);

    string imageFile = imagePrompt.ImageFilePath;
    Console.WriteLine(imageFile);
}
```

### Download to Base64
Alternately you can also generate the image directly to base64 image data in the image result. The code for this is similar.

```csharp
[TestMethod]
public async Task ImageGenerationToBase64Test()
{
    var generator = new OpenAiImageGeneration(OpenAiApiKey);

    var imagePrompt = new ImagePrompt()
    {
        Prompt = "A bear holding on to a snowy mountain peak, waving a beer glass in the air. Poster style, with a black background in goldenrod line art",
        ImageSize = "1024x1024",
        ImageQuality = "standard",
        ImageStyle = "vivid"
    };

    // Generate with base64 Output Format specified
    bool result = await generator.Generate(imagePrompt, 
                        outputFormat: ImageGenerationOutputFormats.Base64);
    
    // Generate and set properties on `imagePrompt` instance
    Assert.IsTrue(result, generator.ErrorMessage);

    // prompt returns an array of images, but for Dall-e-3 it's always one
    // so FirstImage returns the first image.
    byte[] bytes =  imagePrompt.GetBytesFromBase64();
    Assert.IsNotNull(bytes);

    // Optionally save the image to file - default folder here
    string file = await imagePrompt.SaveImageFromBase64();        
    Assert.IsTrue(File.Exists(file));

    // show image in OS viewer
    Console.WriteLine("File generated: " + file);
    ShellUtils.GoUrl(file);  // show in image viewer
}
```



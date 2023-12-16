using System.ComponentModel;
using System.Runtime.CompilerServices;
using Newtonsoft.Json;
using Westwind.Utilities;

namespace Westwind.Ai;

/// <summary>
/// Open AI Prompt container that holds both the request data
/// and response urls and data if retrieved.
/// </summary>
public class ImagePrompt : INotifyPropertyChanged
{    
    #region Properties
    public string Prompt
    {
        get => _prompt;
        set
        {
            if (value == _prompt) return;
            _prompt = value;
            OnPropertyChanged();
            OnPropertyChanged(nameof(IsEmpty));            
        }
    }

    /// <summary>
    /// If using Variations mode this is the image file to use as a
    /// base for the variation.
    /// </summary>
    public string VariationImageFilePath
    {
        get => _variationImageFile;
        set
        {
            if (value == _variationImageFile) return;
            _variationImageFile = value;
            OnPropertyChanged();
        }
    }

    public string ImageSize
    {
        get => _imageSize;
        set
        {
            if (value == _imageSize) return;
            _imageSize = value;
            OnPropertyChanged();
        }
    }

    public string ImageStyle
    {
        get => _imageStyle;
        set
        {
            if (value == _imageStyle) return;
            _imageStyle = value;
            OnPropertyChanged();
        }
    }

    public string ImageQuality
    {
        get => _imageQuality;
        set
        {
            if (value == _imageQuality) return;
            _imageQuality = value;
            OnPropertyChanged();
        }
    }

    public int ImageCount
    {
        get => _imageCount;
        set
        {
            if (value == _imageCount) return;
            _imageCount = value;
            OnPropertyChanged();
        }
    }

    public string Model
    {
        get => _model;
        set
        {
            if (value == _model) return;
            _model = value;
            OnPropertyChanged();
        }
    }


    public string[] ImageUrls
    {
        get => _imageUrls;
        set
        {
            if (Equals(value, _imageUrls)) return;
            _imageUrls = value;
            OnPropertyChanged();
            OnPropertyChanged(nameof(FirstImageUrl));
            OnPropertyChanged(nameof(FirstImageUri));
        }
    }

    public string RevisedPrompt
    {
        get => _revisedPrompt;
        set
        {
            if (value == _revisedPrompt) return;
            _revisedPrompt = value;
            OnPropertyChanged();
            OnPropertyChanged(nameof(HasRevisedPrompt));
        }
    }


    /// <summary>
    /// Name of a captured image. File only, no path
    /// </summary>
    public string ImageFileName
    {
        get => _imageFileName;
        set
        {
            if (value == _imageFileName) return;
            _imageFileName = value;
            OnPropertyChanged();
            OnPropertyChanged(nameof(ImageFilePath));
            OnPropertyChanged(nameof(HasImageFile));            
        }
    }

    /// <summary>
    /// The full path to the image associated with this
    /// Prompt or null/empty.
    /// </summary>
    [JsonIgnore]
    public string ImageFilePath
    {
        get
        {
            if (string.IsNullOrEmpty(ImageFileName))
                return ImageFileName;

            return GetImageFilename(ImageFileName);
        }
    }


    

    [JsonIgnore]
    public string FirstImageUrl {
        get
        {
            return  ImageUrls.FirstOrDefault();
        }
    }

    [JsonIgnore]
    public Uri FirstImageUri
    {
        get
        {
            var img =  ImageUrls.FirstOrDefault();
            return img != null ? new Uri(img) : null;
        }
    }


    public bool HasRevisedPrompt =>
        !string.IsNullOrEmpty(RevisedPrompt);

    public bool IsEmpty =>
        string.IsNullOrEmpty(Prompt) &&
        string.IsNullOrEmpty(RevisedPrompt) &&
        string.IsNullOrEmpty(ImageFileName) &&
        ImageUrls == null;

    public bool HasImageFile =>
        !string.IsNullOrEmpty(ImageFileName)
        && File.Exists(ImageFilePath);


    public string ImageFolderPath
    {
        get { return _imageFolderPath; }
        set
        {
            if (value == _imageFolderPath) return;
            _imageFolderPath = value;
            OnPropertyChanged();
        }
    }

    #endregion


    #region Image Access

    /// <summary>
    /// Retrieves Base64 Image data from the saved file in
    /// Image Url data format that can be embedded into
    /// a document.
    /// </summary>
    /// <param name="filename"></param>
    /// <returns></returns>
    public async Task<string> GetBase64DataFromImageFile(string filename = null)
    {        
        var fname = GetImageFilename(filename);
        if (File.Exists(fname))
        {
            var bytes = await File.ReadAllBytesAsync(fname);
            return HtmlUtils.BinaryToEmbeddedBase64(bytes, "image/png");
        }

        return null;
    }


    /// <summary>
    /// Retrieves bytes from the image file
    /// </summary>
    /// <param name="filename"></param>
    /// <returns></returns>
    public async Task<byte[]> GetBytesFromImageFile(string filename = null)
    {
        var fname = GetImageFilename(filename);
        
        if (File.Exists(fname))
        {
            return await File.ReadAllBytesAsync(fname);
        }

        return null;
    }

    /// <summary>
    /// This method fixes up the image file name to the image
    /// path. If a full path is provided it's used as is.    
    /// </summary>
    /// <param name="fileOnlyName">filename to resolve. If omitted ImageFilename is used</param>
    /// <returns></returns>
    public string GetImageFilename(string fileOnlyName = null)
    {
        if (string.IsNullOrEmpty(fileOnlyName))
            fileOnlyName = ImageFileName;
        if (string.IsNullOrEmpty(fileOnlyName))
            return fileOnlyName;

        if (fileOnlyName.Contains('\\'))
            return fileOnlyName;

        return Path.Combine(ImageFolderPath, fileOnlyName);
    }

    /// <summary>
    /// Downloads image to file based on an image url (or the first image if not provided)
    /// * Downloads file
    /// * Saves into image folder
    /// * Sets Filename to the file downloaded (filename only)
    /// </summary>
    /// <returns>true or false</returns>
    public async Task<bool> DownloadImageToFile(string imageurl = null)
    {
        if (string.IsNullOrEmpty(imageurl))
            imageurl = FirstImageUrl;

        byte[] data = null;
        try
        {
            data = await HttpUtils.HttpRequestBytesAsync(FirstImageUrl);            
        }
        catch
        {            
            return false;
        }

        try
        {
            ImageFileName = await WriteDataToImageFileAsync(data);
        }
        catch
        {
            return false;
        }

        return true;
    }

    /// <summary>
    /// Writes binary data to an image file in the image file folder
    /// </summary>
    /// <param name="data">binary data to write</param>
    /// <returns>returns the file name only (no path)</returns>
    public async Task<string> WriteDataToImageFileAsync(byte[] data)
    {
        var shortFilename = "_" + DataUtils.GenerateUniqueId(8) + ".png";        
        
        if (!Directory.Exists(ImageFolderPath))
            Directory.CreateDirectory(ImageFolderPath);
        var filename = Path.Combine(ImageFolderPath, shortFilename);

        await File.WriteAllBytesAsync(filename, data);

        return shortFilename;
    }

    public ImagePrompt CopyFrom(ImagePrompt existing = null, bool noImageData = false)
    {
        if (existing == null)
            existing = new ImagePrompt();

        Prompt = existing.Prompt;
        RevisedPrompt = existing.RevisedPrompt;
        ImageSize = existing.ImageSize ?? "1024x1024";
        ImageQuality = existing.ImageQuality ?? "standard";
        ImageStyle = existing.ImageStyle ?? "vivid";
        Model = existing.Model ?? "dall-e-3";
       

        if (!noImageData)
        {
            ImageUrls = existing.ImageUrls;
        }
        
        return existing;
    }

    public override string ToString()
    {
        if (string.IsNullOrEmpty(Prompt))
            return "(empty ImagePrompt)";

        return StringUtils.TextAbstract(Prompt, 45);
    }

    #endregion

    #region Property Changed
    public event PropertyChangedEventHandler PropertyChanged;

    protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }

    protected bool SetField<T>(ref T field, T value, [CallerMemberName] string propertyName = null)
    {
        if (EqualityComparer<T>.Default.Equals(field, value)) return false;
        field = value;
        OnPropertyChanged(propertyName);
        return true;
    }

    #endregion

    #region fields
    private string[] _imageUrls = new string[] { };
    private string _prompt;
    private string _imageSize = "1024x1024";
    private int _imageCount = 1;
    private string _imageStyle = "vivid";  // natural
    private string _imageFileName;
    private string _model = "dall-e-3";
    private string _imageQuality = "standard";
    private string _variationImageFile;
    private string _revisedPrompt;

    private string _imageFolderPath = Path.Combine(Path.GetTempPath(), "OpenAi-Images", "Images");


    #endregion

}

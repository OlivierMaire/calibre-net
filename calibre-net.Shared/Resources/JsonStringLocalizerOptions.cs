using Microsoft.Extensions.Localization;

namespace Calibre_net.Shared.Resources;

public class JsonStringLocalizerOptions : LocalizationOptions{

    
public bool ShowKeyNameIfEmpty { get; set;}
public bool ShowDefaultIfEmpty { get; set;}

}
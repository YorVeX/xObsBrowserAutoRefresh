# xObsBrowserAutoRefresh
OBS plugin providing a filter for automatically refreshing a browser source in a configurable interval.

![image](https://user-images.githubusercontent.com/528974/218346677-28e55647-9f63-4818-a547-7d751a755172.png)


## Prerequisites
- OBS 29+ 64 bit
- Currently only working on Windows (tested only on Windows 10, but Windows 11 should also work)

## Usage
After installation [open the filter settings](https://obsproject.com/wiki/Filters-Guide) for a browser source and add the "Browser Auto-refresh" filter, then configure the refresh interval to your liking.

![image](https://user-images.githubusercontent.com/528974/218328552-299a2016-5b1d-40e2-8adc-31e9f398caba.png)

Note that the filter will be active and browser refreshes will be triggered even if the eye icon next to the filter is disabled. Only when the browser source it was added to is hidden the filter will no longer trigger auto-refreshes on it.


## FAQ
- **Q**: Why is the plugin file so big compared to other plugins for the little bit it does, will this cause issues?
  - **A**: Unlike other plugins it's not written directly in C++ but in C# using .NET 7 and NativeAOT (for more details read on in the section for developers). This produces some overhead in the actual plugin file, however, the code that matters for functionality of this plugin should be just as efficient and fast as code directly written in C++ so there's no reason to worry about performance on your system.

- **Q**: Will there be a version for other operating systems, e.g. Linux or MacOS?
  - **A**: The project can already be built on Linux and produce a plugin file, however, trying to load it makes OBS crash on startup and I don't know why, since I don't have any Linux debugging experience. If you think you can help this would be much appreciated, [please start here](https://github.com/YorVeX/ObsCSharpExample/issues/2).
  MacOS is even more complicated, since it [is currently only supported by the next preview version of .NET 8](https://learn.microsoft.com/en-us/dotnet/core/deploying/native-aot/#platformarchitecture-restrictions), although people [do already successfully create builds](https://github.com/dotnet/runtime/issues/79253) with it. This will also need help from the community, I won't work on that myself.

- **Q**: Will there be a 32 bit version of this plugin?
  - **A**: No. Feel free to try and compile it for x86 targets yourself, last time I checked it wasn't fully supported in NativeAOT.

- **Q**: What happens if I accidentally add this filter to a source that is not a browser source?
  - **A**: Don't worry, nothing will explode. The plugin doesn't explicitly check for the source it's added to to be a browser source, but it searches for refresh button of the plugin and "clicks on it". If it doesn't find that button with the internal ID "refreshnocache" it will simply not do the refresh and instead write an error message about this to the OBS log. It was made that way so that the plugin would also work on modified/similar browser sources as long as they provide a button with internal "refreshnocache" ID.

## For developers
### C#
OBS Classic still had a [CLR Host Plugin](https://obsproject.com/forum/resources/clr-host-plugin.21/), but with OBS Studio writing plugins in C# wasn't possible anymore. This has changed as of recently.
The catch about this plugin is that at its day of release (January 12, 2023) to my knowledge it's the first OBS Studio plugin ever written in C# that has some real-world use. The very first without real-world use was [this](https://github.com/kostya9/DotnetObsPluginWithNativeAOT) example plugin demonstrating the basic concept of writing an OBS Studio plugin using .NET 7 and NativeAOT.

### Building
Refer to the [building instructions for my example plugin](https://github.com/YorVeX/ObsCSharpExample#building), they will also apply here.

## Credits
Many thanks to [kostya9](https://github.com/kostya9) for laying the groundwork of C# OBS Studio plugin creation, without him this plugin (and hopefully many more C# plugins following in the future) wouldn't exist. Read about his ventures into this area in his blog posts [here](https://sharovarskyi.com/blog/posts/dotnet-obs-plugin-with-nativeaot/) and [here](https://sharovarskyi.com/blog/posts/clangsharp-dotnet-interop-bindings/). 

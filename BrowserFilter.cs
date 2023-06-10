// SPDX-FileCopyrightText: © 2023 YorVeX, https://github.com/YorVeX
// SPDX-License-Identifier: MIT

using System.Runtime.InteropServices;
using System.Text;
using ObsInterop;
namespace xObsBrowserAutoRefresh;

public class BrowserFilter
{
  public unsafe struct Context
  {
    public obs_source* Source;
    public obs_data* Settings;
    public bool Active;
    public float SecondsWaited;
    public int RefreshIntervalSeconds;
  }

  #region Helper methods
  public static unsafe void Register()
  {
    var sourceInfo = new obs_source_info();
    fixed (byte* id = Encoding.UTF8.GetBytes(Module.ModuleName + " Browser Filter"))
    {
      sourceInfo.id = (sbyte*)id;
      sourceInfo.type = obs_source_type.OBS_SOURCE_TYPE_FILTER;
      sourceInfo.output_flags = ObsSource.OBS_SOURCE_VIDEO;
      sourceInfo.get_name = &filter_get_name;
      sourceInfo.create = &filter_create;
      sourceInfo.show = &filter_show;
      sourceInfo.hide = &filter_hide;
      sourceInfo.destroy = &filter_destroy;
      sourceInfo.get_defaults = &filter_get_defaults;
      sourceInfo.get_properties = &filter_get_properties;
      sourceInfo.save = &filter_save;
      sourceInfo.video_tick = &filter_tick;
      ObsSource.obs_register_source_s(&sourceInfo, (nuint)Marshal.SizeOf(sourceInfo));
    }
  }

  public static unsafe void RefreshBrowserSource(Context* context)
  {
    //TODO: feature: freeze the source display for a configurable amount of time during refresh to prevent flickering
    Module.Log("RefreshBrowserSource called", ObsLogLevel.Debug);
    var browserSource = Obs.obs_filter_get_parent(context->Source);
    if ((browserSource == null) || !Convert.ToBoolean(Obs.obs_source_active(browserSource)))
      return;

    Task.Run(() =>
    {
      var sourceProperties = Obs.obs_source_properties(browserSource);
      fixed (byte* refreshButtonId = "refreshnocache"u8)
      {
        var property = ObsProperties.obs_properties_get(sourceProperties, (sbyte*)refreshButtonId);
        if (property != null) // could be null if this filter is applied on something that is not a browser source
        {
          Module.Log("Refreshing browser...", ObsLogLevel.Debug);
          ObsProperties.obs_property_button_clicked(property, browserSource);
        }
        else
        {
          string sourceDisplayName = Marshal.PtrToStringUTF8((IntPtr)Obs.obs_source_get_name(browserSource))!;
          Module.Log("Failed to refresh source \"" + sourceDisplayName + "\", is this filter really applied to a browser source?", ObsLogLevel.Error);
        }
        ObsProperties.obs_properties_destroy(sourceProperties);
      }
    });
  }
  #endregion Helper methods

  #region Filter API methods
  [UnmanagedCallersOnly(CallConvs = new[] { typeof(System.Runtime.CompilerServices.CallConvCdecl) })]
  public static unsafe sbyte* filter_get_name(void* data)
  {
    Module.Log("filter_get_name called", ObsLogLevel.Debug);
    fixed (byte* logMessagePtr = "Browser Auto-refresh"u8)
      return (sbyte*)logMessagePtr;
  }

  [UnmanagedCallersOnly(CallConvs = new[] { typeof(System.Runtime.CompilerServices.CallConvCdecl) })]
  public static unsafe void* filter_create(obs_data* settings, obs_source* source)
  {
    Module.Log("filter_create called", ObsLogLevel.Debug);

    var context = ObsBmem.bzalloc<Context>();
    context->Source = source;
    context->Settings = settings;
    fixed (byte* intervalId = "interval"u8)
      context->RefreshIntervalSeconds = (int)ObsData.obs_data_get_int(settings, (sbyte*)intervalId);
    return (void*)context;
  }

  [UnmanagedCallersOnly(CallConvs = new[] { typeof(System.Runtime.CompilerServices.CallConvCdecl) })]
  public static unsafe void filter_show(void* data)
  {
    Module.Log("filter_show called", ObsLogLevel.Debug);

    var context = (Context*)data;
    context->SecondsWaited = 0; // if a browser refresh on show is wanted this can be configured in the browser source, the assumption here is that the interval starts from the time the browser source is being shown
    context->Active = true;
  }

  [UnmanagedCallersOnly(CallConvs = new[] { typeof(System.Runtime.CompilerServices.CallConvCdecl) })]
  public static unsafe void filter_hide(void* data)
  {
    Module.Log("filter_hide called", ObsLogLevel.Debug);
    ((Context*)data)->Active = false;
  }

  [UnmanagedCallersOnly(CallConvs = new[] { typeof(System.Runtime.CompilerServices.CallConvCdecl) })]
  public static unsafe void filter_destroy(void* data)
  {
    Module.Log("filter_destroy called", ObsLogLevel.Debug);
    ObsBmem.bfree(data);
  }

  [UnmanagedCallersOnly(CallConvs = new[] { typeof(System.Runtime.CompilerServices.CallConvCdecl) })]
  public static unsafe obs_properties* filter_get_properties(void* data)
  {
    Module.Log("filter_get_properties called", ObsLogLevel.Debug);

    var properties = ObsProperties.obs_properties_create();
    fixed (byte*
      intervalId = "interval"u8,
      intervalCaption = Module.ObsText("IntervalCaption"),
      intervalText = Module.ObsText("IntervalText")
    )
    {
      var prop = ObsProperties.obs_properties_add_int(properties, (sbyte*)intervalId, (sbyte*)intervalCaption, 1, int.MaxValue, 1);
      ObsProperties.obs_property_set_long_description(prop, (sbyte*)intervalText);
    }
    return properties;
  }

  [UnmanagedCallersOnly(CallConvs = new[] { typeof(System.Runtime.CompilerServices.CallConvCdecl) })]
  public static unsafe void filter_get_defaults(obs_data* settings)
  {
    Module.Log("filter_get_defaults called", ObsLogLevel.Debug);
    fixed (byte* intervalId = "interval"u8)
      ObsData.obs_data_set_default_int(settings, (sbyte*)intervalId, 60);
  }

  [UnmanagedCallersOnly(CallConvs = new[] { typeof(System.Runtime.CompilerServices.CallConvCdecl) })]
  public static unsafe void filter_save(void* data, obs_data* settings)
  {
    var context = (Context*)data;
    Module.Log("filter_save called", ObsLogLevel.Debug);
    fixed (byte* intervalId = "interval"u8)
      context->RefreshIntervalSeconds = (int)ObsData.obs_data_get_int(settings, (sbyte*)intervalId);
    Module.Log("Browser auto refresh interval was set to " + context->RefreshIntervalSeconds + " second(s)", ObsLogLevel.Debug);
    
    var browserSource = Obs.obs_filter_get_parent(context->Source);
    context->Active = (browserSource != null) && Convert.ToBoolean(Obs.obs_source_active(browserSource)); // this ensures any newly added filter to immediately start refreshing if the browser source is visible
  }

  [UnmanagedCallersOnly(CallConvs = new[] { typeof(System.Runtime.CompilerServices.CallConvCdecl) })]
  public static unsafe void filter_tick(void* data, float seconds)
  {
    var context = (Context*)data;
    if (context->Active && ((context->SecondsWaited += seconds) >= context->RefreshIntervalSeconds))
    {
      context->SecondsWaited -= context->RefreshIntervalSeconds;
      RefreshBrowserSource(context);
    }
  }
  #endregion Filter API methods


}
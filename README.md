# VolumeWatcher - protects your ears at (almost) all costs

A simple (**Windows only**) console application which will automatically mute Processes (Audio Sessions) if they cross a certain volume threshold.
<br/>
<br/>
The Peak Volume Threshold is adjustable within a normalized range up to 1.0, where 1.0 represents 100% of the ***relative***
<br/>
volume capacity or 0dBFS.
<br/>
<br/>
Relative Volume meaning Windows' Volume Mixer limits will be taken into account.
<br/>
<br/>
The rate at which the Sessions will be watched can also be adjusted, as well as a few other options.
<br/>
<br/>
> [!NOTE]
> Finding the perfect Peak Volume Threshold involves some trial and error and varies greatly depending on your hardware setup.
> <br/>
> <br/>
> Additionally, adjusting the volume through external means, like hardware controls, can make it even harder to pinpoint
> the correct threshold, or sometimes, it might not be possible at all.
<br/>

### TODO
- [ ] Discover a more efficient method than mere polling the Audio Sessions
- [ ] Ability to watch multiple Devices at once
<br/>

---

<br/>
Implemented in C# using NAudio for the [WASAPI](https://learn.microsoft.com/en-us/windows/win32/coreaudio/wasapi) Wrapper.
<br/>
<br/>

![ui example](https://i.imgur.com/0JcUre3.png)

using System;
using System.Diagnostics;
using System.Text.RegularExpressions;

namespace IctBaden.Stonehenge.Core;

internal class SimpleUserAgentDecoder
{
    public string BrowserName = string.Empty;
    public string BrowserVersion = string.Empty;

    public string ClientOsName = string.Empty;
    public string ClientOsVersion = string.Empty;

    public SimpleUserAgentDecoder(string userAgent)
    {
        if (string.IsNullOrEmpty(userAgent)) return;

        try
        {
            DetectBrowser(userAgent);
            DetectOperatingSystem(userAgent);
        }
        catch (Exception ex)
        {
            Debug.WriteLine(ex.Message);
            Debugger.Break();
        }
    }

    private void DetectBrowser(string userAgent)
    {
        var edge = new Regex(@"Edg/([0-9\.]+)")
            .Match(userAgent);
        if (edge.Success)
        {
            BrowserName = "Edge";
            BrowserVersion = edge.Groups[1].Value;
            return;
        }

        var chrome = new Regex(@"Chrome/([0-9\\.]+)")
            .Match(userAgent);
        if (chrome.Success)
        {
            BrowserName = "Chrome";
            BrowserVersion = chrome.Groups[1].Value;
            return;
        }

        var firefox = new Regex(@"Firefox/([0-9\.]+)")
            .Match(userAgent);
        if (firefox.Success)
        {
            BrowserName = "Firefox";
            BrowserVersion = firefox.Groups[1].Value;
            return;
        }
        
        BrowserName = userAgent;
    }
 
    private void DetectOperatingSystem(string userAgent)
    {
        var windows = new Regex(@"Windows NT ([0-9\.]+)")
            .Match(userAgent);
        if (windows.Success)
        {
            ClientOsName = "Windows";
            ClientOsVersion = windows.Groups[1].Value;
            return;
        }

        var linux = new Regex(@"(([a-zA-Z]+)|X11); Linux")
            .Match(userAgent);
        if (linux.Success)
        {
            ClientOsName = linux.Groups[1].Value;
            if (ClientOsName == "X11")
            {
                ClientOsName = "Debian";
            }
            else if (string.IsNullOrEmpty(ClientOsName))
            {
                ClientOsName = "Linux";
            }
            return;
        }

        ClientOsName = userAgent;
    }


}
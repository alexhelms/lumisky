using Microsoft.Extensions.DependencyInjection;
using OdinEye.Core;
using OdinEye.Core.Devices;
using OdinEye.Core.Imaging;
using OdinEye.Core.Imaging.Processing;
using OdinEye.Core.Profile;
using Serilog;

namespace ConsoleApp1;

internal class Program
{
    static async Task Main(string[] args)
    {
        Log.Logger = new LoggerConfiguration()
            .MinimumLevel.Verbose()
            .Enrich.FromLogContext()
            .WriteTo.Console()
            .CreateLogger();

        try
        {
            //string deviceName = "ZWO CCD ASI224MC";
            //string hostname = "192.168.7.217";
            //int port = 7624;

            //var serviceCollection = new ServiceCollection();
            //serviceCollection.ConfigureOdinEyeCore();
            //IServiceProvider provider = serviceCollection.BuildServiceProvider();
            //Bootstrap.UseOdinEyeCore(provider);

            //var profileProvider = provider.GetRequiredService<IProfileProvider>();
            //profileProvider.Current.Camera.Name = deviceName;
            //profileProvider.Current.Camera.IndiHostname = hostname;
            //profileProvider.Current.Camera.IndiPort = port;

            //var camera = new IndiCamera(profileProvider);
            //await camera.ConnectAsync();
            //if (!camera.IsConnected)
            //{
            //    Console.WriteLine("Could not connect to camera");
            //    return;
            //}

            //using var image = await camera.TakeImageAsync(
            //    new ExposureParameters
            //    {
            //        Duration = TimeSpan.FromMilliseconds(10),
            //        Gain = 0,
            //        Offset = 10,
            //    });

            //image!.SaveAsFits(@"C:\tmp\indi.fits", ImageOutputType.UInt16, overwrite: true);

            //using var debayeredImage = Debayer.FromFits(@"C:\tmp\indi.fits");
            //Console.WriteLine($"Mean[0] = {debayeredImage.Mean(0)}");
            //Console.WriteLine($"Mean[1] = {debayeredImage.Mean(1)}");
            //Console.WriteLine($"Mean[2] = {debayeredImage.Mean(2)}");

            //debayeredImage.SaveAsFits(@"C:\tmp\indi-debayered.fits", overwrite: true);

            //await camera.DisconnectAsync();
        }
        catch (Exception e)
        {
            Log.Fatal(e, "Exception");
        }
        finally
        {
            await Log.CloseAndFlushAsync();
        }
    }
}
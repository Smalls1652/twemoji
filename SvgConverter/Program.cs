using System.Runtime.InteropServices;
using ImageMagick;
using ImageMagick.ImageOptimizers;

CancellationTokenSource cancellationTokenSource = new();

PosixSignalRegistration sigIntRegistration = PosixSignalRegistration.Create(PosixSignal.SIGINT, HandlePosixSignal);
PosixSignalRegistration sigTermRegistration = PosixSignalRegistration.Create(PosixSignal.SIGTERM, HandlePosixSignal);
PosixSignalRegistration sigQuitRegistration = PosixSignalRegistration.Create(PosixSignal.SIGQUIT, HandlePosixSignal);

try
{
    string svgDirPath = args[0] ?? throw new ArgumentException("SVG directory path is required");
    string outDirPath = args[1] ?? throw new ArgumentException("Output directory path is required");

    string svgDirPathResolved = Path.GetFullPath(svgDirPath);

    if (!Directory.Exists(svgDirPathResolved))
    {
        throw new ArgumentException($"SVG directory path {svgDirPathResolved} does not exist");
    }

    string[] svgFiles = Directory.GetFiles(
        path: svgDirPathResolved,
        searchPattern: "*.svg"
    );

    if (svgFiles.Length == 0)
    {
        throw new ArgumentException($"SVG directory path '{svgDirPathResolved}' does not contain any SVG files");
    }

    string outDirPathResolved = Path.GetFullPath(outDirPath);

    if (Directory.Exists(outDirPathResolved))
    {
        Console.WriteLine($"Deleting existing output directory '{outDirPathResolved}'");
        Directory.Delete(
            path: outDirPathResolved,
            recursive: true
        );
    }

    Directory.CreateDirectory(outDirPathResolved);

    MagickNET.Initialize();

    var cancellationToken = cancellationTokenSource.Token;

    try
    {
        for (int i = 0; i < svgFiles.Length; i++)
        {
            if (cancellationToken.IsCancellationRequested)
            {
                cancellationToken.ThrowIfCancellationRequested();
            }

            string convertedImagePath = Path.Combine(
                path1: outDirPathResolved,
                path2: $"{Path.GetFileNameWithoutExtension(svgFiles[i])}.png"
            );

            using (MagickImage image = new())
            {
                image.Format = MagickFormat.Svg;
                image.BackgroundColor = MagickColors.None;

                image.Read(svgFiles[i], 512, 512);
                image.Density = new Density(420, DensityUnit.PixelsPerInch);
                //image.Resize(512, 512);

                Console.WriteLine($"{Path.GetFileName(svgFiles[i])} -> {convertedImagePath}");
                image.Write(convertedImagePath, MagickFormat.Png32);
            }
        }

        Console.WriteLine("Done! 🤌");
    }
    catch (OperationCanceledException)
    {
        Console.WriteLine("\nOperation cancelled");
    }
    catch (Exception)
    {
        Console.WriteLine("ono sumtin wen wong 👉👈");
        throw;
    }
}
finally
{
    cancellationTokenSource.Dispose();
    sigIntRegistration.Dispose();
    sigTermRegistration.Dispose();
    sigQuitRegistration.Dispose();
}

void HandlePosixSignal(PosixSignalContext context)
{
    context.Cancel = true;
    cancellationTokenSource.Cancel();
}

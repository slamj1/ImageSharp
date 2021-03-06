﻿// Copyright (c) Six Labors and contributors.
// Licensed under the Apache License, Version 2.0.

using System.IO;
using SixLabors.ImageSharp.Formats.Gif;
using SixLabors.ImageSharp.Metadata;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing.Processors.Quantization;
using SixLabors.ImageSharp.Tests.TestUtilities.ImageComparison;
using Xunit;
// ReSharper disable InconsistentNaming

namespace SixLabors.ImageSharp.Tests.Formats.Gif
{
    public class GifEncoderTests
    {
        private const PixelTypes TestPixelTypes = PixelTypes.Rgba32 | PixelTypes.RgbaVector | PixelTypes.Argb32;
        private static readonly ImageComparer ValidatorComparer = ImageComparer.TolerantPercentage(0.0015F);

        public static readonly TheoryData<string, int, int, PixelResolutionUnit> RatioFiles =
        new TheoryData<string, int, int, PixelResolutionUnit>
        {
            { TestImages.Gif.Rings, (int)ImageMetadata.DefaultHorizontalResolution, (int)ImageMetadata.DefaultVerticalResolution , PixelResolutionUnit.PixelsPerInch},
            { TestImages.Gif.Ratio1x4, 1, 4 , PixelResolutionUnit.AspectRatio},
            { TestImages.Gif.Ratio4x1, 4, 1, PixelResolutionUnit.AspectRatio }
        };

        [Theory]
        [WithTestPatternImages(100, 100, TestPixelTypes)]
        public void EncodeGeneratedPatterns<TPixel>(TestImageProvider<TPixel> provider)
            where TPixel : struct, IPixel<TPixel>
        {
            using (Image<TPixel> image = provider.GetImage())
            {
                var encoder = new GifEncoder()
                {
                    // Use the palette quantizer without dithering to ensure results 
                    // are consistant
                    Quantizer = new WebSafePaletteQuantizer(false)
                };

                // Always save as we need to compare the encoded output.
                provider.Utility.SaveTestOutputFile(image, "gif", encoder);
            }

            // Compare encoded result
            string path = provider.Utility.GetTestOutputFileName("gif", null, true);
            using (var encoded = Image.Load<Rgba32>(path))
            {
                encoded.CompareToReferenceOutput(ValidatorComparer, provider, null, "gif");
            }
        }

        [Theory]
        [MemberData(nameof(RatioFiles))]
        public void Encode_PreserveRatio(string imagePath, int xResolution, int yResolution, PixelResolutionUnit resolutionUnit)
        {
            var options = new GifEncoder();

            var testFile = TestFile.Create(imagePath);
            using (Image<Rgba32> input = testFile.CreateRgba32Image())
            {
                using (var memStream = new MemoryStream())
                {
                    input.Save(memStream, options);

                    memStream.Position = 0;
                    using (var output = Image.Load<Rgba32>(memStream))
                    {
                        ImageMetadata meta = output.Metadata;
                        Assert.Equal(xResolution, meta.HorizontalResolution);
                        Assert.Equal(yResolution, meta.VerticalResolution);
                        Assert.Equal(resolutionUnit, meta.ResolutionUnits);
                    }
                }
            }
        }

        [Fact]
        public void Encode_IgnoreMetadataIsFalse_CommentsAreWritten()
        {
            var options = new GifEncoder();

            var testFile = TestFile.Create(TestImages.Gif.Rings);

            using (Image<Rgba32> input = testFile.CreateRgba32Image())
            {
                using (var memStream = new MemoryStream())
                {
                    input.Save(memStream, options);

                    memStream.Position = 0;
                    using (var output = Image.Load<Rgba32>(memStream))
                    {
                        Assert.Equal(1, output.Metadata.Properties.Count);
                        Assert.Equal("Comments", output.Metadata.Properties[0].Name);
                        Assert.Equal("ImageSharp", output.Metadata.Properties[0].Value);
                    }
                }
            }
        }

        [Fact]
        public void Encode_IgnoreMetadataIsTrue_CommentsAreNotWritten()
        {
            var options = new GifEncoder();

            var testFile = TestFile.Create(TestImages.Gif.Rings);

            using (Image<Rgba32> input = testFile.CreateRgba32Image())
            {
                input.Metadata.Properties.Clear();
                using (var memStream = new MemoryStream())
                {
                    input.SaveAsGif(memStream, options);

                    memStream.Position = 0;
                    using (var output = Image.Load<Rgba32>(memStream))
                    {
                        Assert.Equal(0, output.Metadata.Properties.Count);
                    }
                }
            }
        }

        [Fact]
        public void Encode_WhenCommentIsTooLong_CommentIsTrimmed()
        {
            using (var input = new Image<Rgba32>(1, 1))
            {
                string comments = new string('c', 256);
                input.Metadata.Properties.Add(new ImageProperty("Comments", comments));

                using (var memStream = new MemoryStream())
                {
                    input.Save(memStream, new GifEncoder());

                    memStream.Position = 0;
                    using (var output = Image.Load<Rgba32>(memStream))
                    {
                        Assert.Equal(1, output.Metadata.Properties.Count);
                        Assert.Equal("Comments", output.Metadata.Properties[0].Name);
                        Assert.Equal(255, output.Metadata.Properties[0].Value.Length);
                    }
                }
            }
        }

        [Theory]
        [WithFile(TestImages.Gif.Cheers, PixelTypes.Rgba32)]
        public void EncodeGlobalPaletteReturnsSmallerFile<TPixel>(TestImageProvider<TPixel> provider)
            where TPixel : struct, IPixel<TPixel>
        {
            using (Image<TPixel> image = provider.GetImage())
            {
                var encoder = new GifEncoder
                {
                    ColorTableMode = GifColorTableMode.Global,
                    Quantizer = new OctreeQuantizer(false)
                };

                // Always save as we need to compare the encoded output.
                provider.Utility.SaveTestOutputFile(image, "gif", encoder, "global");

                encoder.ColorTableMode = GifColorTableMode.Local;
                provider.Utility.SaveTestOutputFile(image, "gif", encoder, "local");

                var fileInfoGlobal = new FileInfo(provider.Utility.GetTestOutputFileName("gif", "global"));
                var fileInfoLocal = new FileInfo(provider.Utility.GetTestOutputFileName("gif", "local"));

                Assert.True(fileInfoGlobal.Length < fileInfoLocal.Length);
            }
        }

        [Fact]
        public void NonMutatingEncodePreservesPaletteCount()
        {
            using (var inStream = new MemoryStream(TestFile.Create(TestImages.Gif.Leo).Bytes))
            using (var outStream = new MemoryStream())
            {
                inStream.Position = 0;

                var image = Image.Load<Rgba32>(inStream);
                GifMetadata metaData = image.Metadata.GetFormatMetadata(GifFormat.Instance);
                GifFrameMetadata frameMetaData = image.Frames.RootFrame.Metadata.GetFormatMetadata(GifFormat.Instance);
                GifColorTableMode colorMode = metaData.ColorTableMode;
                var encoder = new GifEncoder()
                {
                    ColorTableMode = colorMode,
                    Quantizer = new OctreeQuantizer(frameMetaData.ColorTableLength)
                };

                image.Save(outStream, encoder);
                outStream.Position = 0;

                outStream.Position = 0;
                var clone = Image.Load<Rgba32>(outStream);

                GifMetadata cloneMetaData = clone.Metadata.GetFormatMetadata<GifMetadata>(GifFormat.Instance);
                Assert.Equal(metaData.ColorTableMode, cloneMetaData.ColorTableMode);

                // Gifiddle and Cyotek GifInfo say this image has 64 colors.
                Assert.Equal(64, frameMetaData.ColorTableLength);

                for (int i = 0; i < image.Frames.Count; i++)
                {
                    GifFrameMetadata ifm = image.Frames[i].Metadata.GetFormatMetadata(GifFormat.Instance);
                    GifFrameMetadata cifm = clone.Frames[i].Metadata.GetFormatMetadata(GifFormat.Instance);

                    Assert.Equal(ifm.ColorTableLength, cifm.ColorTableLength);
                    Assert.Equal(ifm.FrameDelay, cifm.FrameDelay);
                }

                image.Dispose();
                clone.Dispose();
            }
        }
    }
}

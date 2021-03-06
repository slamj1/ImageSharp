﻿// Copyright (c) Six Labors and contributors.
// Licensed under the Apache License, Version 2.0.

using SixLabors.ImageSharp.PixelFormats;
using SixLabors.Primitives;

namespace SixLabors.ImageSharp.Processing
{
    /// <summary>
    /// Defines extensions that allow the application of padding operations on an <see cref="Image"/>
    /// using Mutate/Clone.
    /// </summary>
    public static class PadExtensions
    {
        /// <summary>
        /// Evenly pads an image to fit the new dimensions.
        /// </summary>
        /// <param name="source">The source image to pad.</param>
        /// <param name="width">The new width.</param>
        /// <param name="height">The new height.</param>
        /// <returns>The <see cref="IImageProcessingContext"/> to allow chaining of operations.</returns>
        public static IImageProcessingContext Pad(this IImageProcessingContext source, int width, int height)
        {
            var options = new ResizeOptions
            {
                Size = new Size(width, height),
                Mode = ResizeMode.BoxPad,
                Sampler = KnownResamplers.NearestNeighbor,
            };

            return source.Resize(options);
        }
    }
}
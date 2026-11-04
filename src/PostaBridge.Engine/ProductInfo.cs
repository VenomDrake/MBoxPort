// SPDX-License-Identifier: GPL-3.0-or-later
namespace PostaBridge.Engine;

public static class ProductInfo
{
    public static string Name { get; } =
        System.Reflection.CustomAttributeExtensions.GetCustomAttribute<System.Reflection.AssemblyProductAttribute>(
            typeof(ProductInfo).Assembly)?.Product ?? "MboxPort";

    public static string Version { get; } =
        typeof(ProductInfo).Assembly.GetName().Version?.ToString(3) ?? "0.0.0";
}

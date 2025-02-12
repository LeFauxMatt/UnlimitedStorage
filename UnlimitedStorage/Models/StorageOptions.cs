using System.Globalization;
using System.Text;
using LeFauxMods.Common.Interface;
using LeFauxMods.Common.Models;

namespace LeFauxMods.UnlimitedStorage.Models;

/// <inheritdoc />
internal sealed class StorageOptions : DictionaryDataModel
{
    /// <inheritdoc />
    /// <param name="dictionaryModel">The backing dictionary.</param>
    public StorageOptions(IDictionaryModel? dictionaryModel = null)
        : base(dictionaryModel ?? new DictionaryModel())
    {
        if (this.GetData()?.Any() != false)
        {
            return;
        }

        this.Capacity = -1;
        this.Enabled = true;
        this.MenuHeight = 5;
        this.MenuWidth = 14;
        this.Unlimited = true;
    }

    /// <summary>Gets or sets the storage capacity.</summary>
    public int Capacity
    {
        get => this.Get(nameof(this.Capacity), StringToInt);
        set => this.Set(nameof(this.Capacity), value, IntToString);
    }

    /// <summary>Gets or sets a value indicating whether this storage is enabled.</summary>
    public bool Enabled
    {
        get => this.Get(nameof(this.Enabled), StringToBool);
        set => this.Set(nameof(this.Enabled), value, BoolToString);
    }

    /// <summary>Gets or sets the menu height.</summary>
    public int MenuHeight
    {
        get => this.Get(nameof(this.MenuHeight), StringToInt);
        set => this.Set(nameof(this.MenuHeight), value, IntToString);
    }

    /// <summary>Gets or sets the menu width.</summary>
    public int MenuWidth
    {
        get => this.Get(nameof(this.MenuWidth), StringToInt);
        set => this.Set(nameof(this.MenuWidth), value, IntToString);
    }

    /// <summary>Gets or sets a value indicating whether this storage is unlimited.</summary>
    public bool Unlimited
    {
        get => this.Get(nameof(this.Unlimited), StringToBool);
        set => this.Set(nameof(this.Unlimited), value, BoolToString);
    }

    /// <inheritdoc />
    protected override string Prefix => ModConstants.Prefix;

    /// <summary>Get a summary of the storage's configuration options.</summary>
    /// <returns>Returns the summary.</returns>
    public string GetSummary() =>
        new StringBuilder()
            .AppendLine(CultureInfo.InvariantCulture,
                $"{nameof(this.Capacity),25}: {this.Capacity}")
            .AppendLine(CultureInfo.InvariantCulture,
                $"{nameof(this.Enabled),25}: {this.Enabled}")
            .AppendLine(CultureInfo.InvariantCulture,
                $"{nameof(this.MenuHeight),25}: {this.MenuHeight}")
            .AppendLine(CultureInfo.InvariantCulture,
                $"{nameof(this.MenuWidth),25}: {this.MenuWidth}")
            .AppendLine(CultureInfo.InvariantCulture,
                $"{nameof(this.Unlimited),25}: {this.Unlimited}")
            .ToString();
}
using System.Globalization;
using System.Text;
using LeFauxMods.Common.Interface;
using LeFauxMods.Common.Models;

namespace LeFauxMods.UnlimitedStorage.Models;

/// <inheritdoc />
internal sealed class StorageOptions : DictionaryDataModel
{
    public StorageOptions()
        : this(new DictionaryModel())
    {
    }

    public StorageOptions(StorageSize storageSize)
        : this()
    {
        switch (storageSize)
        {
            case StorageSize.Small:
                this.MenuWidth = 3;
                this.MenuHeight = 3;
                this.Capacity = this.MenuWidth * this.MenuHeight;
                break;

            case StorageSize.Medium:
                this.MenuWidth = 12;
                this.MenuHeight = 3;
                this.Capacity = this.MenuWidth * this.MenuHeight;
                break;

            case StorageSize.Large:
                this.MenuWidth = 14;
                this.MenuHeight = 5;
                this.Capacity = this.MenuWidth * this.MenuHeight;
                break;

            case StorageSize.Unlimited:
                this.MenuWidth = 14;
                this.MenuHeight = 5;
                this.Capacity = -1;
                break;
        }
    }

    /// <inheritdoc />
    /// <param name="dictionaryModel">The backing dictionary.</param>
    public StorageOptions(IDictionaryModel dictionaryModel)
        : base(dictionaryModel)
    {
        if (this.GetData()?.Any() != false)
        {
            return;
        }

        this.Enabled = true;
        this.MenuWidth = 14;
        this.MenuHeight = 5;
        this.Capacity = -1;
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

    /// <inheritdoc />
    protected override string Prefix => ModConstants.Prefix;

    /// <summary>Gets the maximum offset value.</summary>
    /// <param name="count">The number of items.</param>
    /// <returns>Returns the maximum offset.</returns>
    public int GetMaxOffset(int count) =>
        (int)Math.Ceiling((float)count / Math.Min(5, this.MenuHeight)) - Math.Min(14, this.MenuWidth);

    /// <summary>Get a summary of the storage's configuration options.</summary>
    /// <returns>Returns the summary.</returns>
    public string GetSummary() =>
        new StringBuilder()
            .AppendLine(CultureInfo.InvariantCulture,
                $"{nameof(this.Enabled),25}: {this.Enabled}")
            .AppendLine(CultureInfo.InvariantCulture,
                $"{nameof(this.Capacity),25}: {this.Capacity}")
            .AppendLine(CultureInfo.InvariantCulture,
                $"{nameof(this.MenuHeight),25}: {this.MenuHeight}")
            .AppendLine(CultureInfo.InvariantCulture,
                $"{nameof(this.MenuWidth),25}: {this.MenuWidth}")
            .ToString();
}
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
}
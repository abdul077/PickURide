using System;
using System.Collections.Generic;

namespace PickURide.Infrastructure.Data.Entities;

public partial class FareDistanceSlab
{
    public int SlabId { get; set; }

    public int SettingId { get; set; }

    public decimal FromKm { get; set; }

    public decimal? ToKm { get; set; }

    public decimal RatePerKm { get; set; }

    public int SortOrder { get; set; }

    public virtual FareSetting Setting { get; set; } = null!;
}


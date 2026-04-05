using System;
using System.Collections.Generic;

namespace PickURide.Infrastructure.Data.Entities;

public partial class FareSetting
{
    public int SettingId { get; set; }

    public string? AreaType { get; set; }

    public decimal? BaseFare { get; set; }

    public decimal? PerKmRate { get; set; }

    public decimal? PerMinuteRate { get; set; }

    public decimal? AdminPercentage { get; set; }

    public virtual ICollection<FareDistanceSlab> FareDistanceSlabs { get; set; } = new List<FareDistanceSlab>();
}

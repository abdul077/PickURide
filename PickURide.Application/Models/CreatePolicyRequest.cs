namespace PickURide.Application.Models;

public class CreatePolicyRequest
{
    public string? Title { get; set; }

    public string Content { get; set; } = null!;
}


using System;

namespace UniVerse.Web.Models;

/// <summary>
/// Abstract base entity demonstrating C# Object-Oriented Inheritance.
/// Serves as common contract for domain entities (User, Product, DeliveryRequest).
/// </summary>
public abstract class BaseEntity
{
    public int Id { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}

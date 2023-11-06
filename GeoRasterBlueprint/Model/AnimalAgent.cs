using NetTopologySuite.Geometries;

namespace GeoRasterBlueprint.Model;

public interface IAnimalAgent
{
    int UniqueId { get; }
    int Age { get; }
    Point Location { get; }
    float Health { get; }
    string Gender { get; }

    void Move(Point destination);
    void Reproduce(IAnimalAgent partner);
    void Interact(IAnimalAgent agent);
    void Eat();
    void Die();
    void Drink();
}
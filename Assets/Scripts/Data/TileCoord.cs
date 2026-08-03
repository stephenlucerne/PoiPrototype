using System;

namespace Data
{
    public struct TileCoord : IEquatable<TileCoord>
    {
        public readonly int x;
        public readonly int y;

        public TileCoord(int x, int y)
        {
            this.x = x;
            this.y = y;
        }

        public bool Equals(TileCoord other)
        {
            return x == other.x && y == other.y;
        }

        public override bool Equals(object obj)
        {
            return obj is TileCoord other && Equals(other);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                return (x * 397) ^ y;
            }
        }

        public static bool operator ==(TileCoord left, TileCoord right)
        {
            return left.Equals(right);
        }

        public static bool operator !=(TileCoord left, TileCoord right)
        {
            return !left.Equals(right);
        }
    }
}
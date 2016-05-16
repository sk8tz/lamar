using System;

namespace Lamar
{
    internal class BlockMarker : IDisposable
    {
        private readonly SourceWriter _parent;

        public BlockMarker(SourceWriter parent)
        {
            _parent = parent;
        }

        public void Dispose()
        {
            _parent.FinishBlock();
        }
    }
}
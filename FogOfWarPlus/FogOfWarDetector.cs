/* Copyright (c) 2019 Jeremy Buck (jarmo@netcodez.com) http://github.com/devjarmo 
 
Permission is hereby granted, free of charge, to any person obtaining a copy of this software
and associated documentation files (the "Software"), to deal in the Software without
restriction, including without limitation the rights to use, copy, modify, merge, publish,
distribute, sublicense, and/or sell copies of the Software, and to permit persons to whom
the Software is furnished to do so, subject to the following conditions:
The above copyright notice and this permission notice shall be included in all copies or
substantial portions of the Software.

THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR IMPLIED,
INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY, FITNESS FOR A PARTICULAR
PURPOSE AND NON-INFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE
LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION OF CONTRACT,
TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE
USE OR OTHER DEALINGS IN THE SOFTWARE.

*/
using Xenko.Core.Mathematics;
using Xenko.Engine;
// ReSharper disable ClassNeverInstantiated.Global

namespace FogOfWarPlus
{
    public class FogOfWarDetector : StartupScript
    {
        internal string Name;
        internal Vector3 Pos
        {
            get
            {
                Entity.Transform.GetWorldTransformation(out var pos, out _, out _);
                return pos;
            }
        }

        public override void Start()
        {
            Name = Entity.GetParent().GetParent().Name;
            Entity.Get<SpriteComponent>().Enabled = true;
            Services.GetService<FogOfWarSystem>().AddDetector(this);
        }

        public override void Cancel()
        {
            base.Cancel();
            Services.GetService<FogOfWarSystem>().RemoveDetector(this);
        }
    }
}

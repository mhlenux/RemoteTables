using System;

namespace SharedLibrary
{
    public interface ICloseable
    {
        public void OnProcessExit(object sender, EventArgs e);
    }
}

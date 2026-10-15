using System;

namespace Bloomlings.Playtest.Design
{
    /// <summary>Short-lived particles may arrive asynchronously without holding an otherwise complete page.</summary>
    public interface ITransientPictures
    {
        void Transient(Action draw);
    }
}

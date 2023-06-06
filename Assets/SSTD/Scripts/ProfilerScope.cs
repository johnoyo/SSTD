using System;
using UnityEngine;

public class ProfilerScope : IDisposable
{
    float m_StartTime;
    float m_ElapsedTime;
    private string m_Label;
    private bool m_Disposed = false;

    public ProfilerScope(string label)
    {
        m_StartTime = Time.realtimeSinceStartup;
        m_Label = label;
    }


    protected virtual void Dispose(bool disposing)
    {
        if (!m_Disposed)
        {
            if (disposing)
            {
                // ...
            }
            m_Disposed = true;
        }

        m_ElapsedTime = (Time.realtimeSinceStartup - m_StartTime);
        Debug.Log(m_Label + " execution time: " + (m_ElapsedTime * 1000.0f) + "ms");
    }

    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    ~ProfilerScope()
    {
        Dispose(false);
    }
}

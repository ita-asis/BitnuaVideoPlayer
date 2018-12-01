using System;

namespace BitnuaVideoPlayer.UI.AttachedProps
{
    public struct DelayExecuter
    {
        private readonly TimeSpan m_Delay;
        private DateTime m_LastCall;
        public DelayExecuter(int delay)
        {
            m_LastCall = DateTime.MinValue;
            m_Delay = TimeSpan.FromMilliseconds(delay);
        }

        public void Exec(Action action)
        {
            if (m_LastCall != DateTime.MinValue && m_LastCall + m_Delay < DateTime.Now)
                action();
            else
                m_LastCall = DateTime.Now;
        }
    }
}
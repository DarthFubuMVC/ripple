using System;
using System.Collections.Generic;
using FubuCore.Logging;
using NUnit.Framework;
using System.Linq;

namespace ripple.Testing
{
    public class AssertLogListener : ILogListener, IDisposable
    {
        private readonly List<object> expectedMessages = new List<object>();
        private bool inAnyOrder;

        public AssertLogListener()
        {
            IsDebugEnabled = true;
            IsInfoEnabled = true;
        }

        public AssertLogListener InAnyOrder()
        {
            inAnyOrder = true;
            return this;
        }

        public AssertLogListener Expect(object message)
        {
            expectedMessages.Add(message);
            return this;
        }

        public bool ListensFor(Type type)
        {
            return true;
        }

        public void DebugMessage(object message)
        {
            assertMessage(message);
        }

        public void InfoMessage(object message)
        {
            assertMessage(message);
        }

        public void Debug(string message)
        {
            assertMessage(message);
        }

        public void Info(string message)
        {
            assertMessage(message);
        }

        public void Error(string message, Exception ex)
        {
            assertMessage(message);
        }

        public void Error(object correlationId, string message, Exception ex)
        {
            assertMessage(message);
        }

        private void assertMessage(object message)
        {
            if (inAnyOrder)
            {
                Assert.True(expectedMessages.Remove(message), "Message {0} was not found", message );
            }
            else
            {
                var expectedMessage = expectedMessages.First();
                expectedMessages.RemoveAt(0);
                Assert.AreEqual(expectedMessage, message);
            }
        }

        public bool IsDebugEnabled { get; set; }

        public bool IsInfoEnabled { get; set; }

        public void Dispose()
        {
            Assert.AreEqual(0, expectedMessages.Count, "There should be no messages left");
        }
    }
}
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Application.Services;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace Application.Tests.Auth
{
    public class EmailServiceTests
    {
        [Fact]
        public void NotConfigured_DoesNotSend_SameAsDevFallback()
        {
            var service = CreateService(out var transport);
            var result = service.SendPasswordResetEmailAsync("user@example.com", "https://localhost/reset?token=abc");

            Assert.True(result.IsCompleted);
            Assert.Equal(0, transport.CallCount);
        }

        [Fact]
        public async Task Enabled_DelegatesToTransport_WithResetLink()
        {
            var service = CreateService(out var transport,
                ("Email:Enabled", "true"),
                ("Email:SmtpHost", "smtp.example.com"),
                ("Email:SmtpPort", "587"),
                ("Email:Username", "smtp-user"),
                ("Email:Password", "secret-pass"),
                ("Email:FromAddress", "noreply@example.com"),
                ("Email:FromDisplayName", "AIShopVerse"),
                ("Email:UseSsl", "true"));

            await service.SendPasswordResetEmailAsync("recipient@example.com", "https://localhost/reset-password?token=123&email=recipient@example.com");

            Assert.Equal(1, transport.CallCount);
            Assert.NotNull(transport.LastMessage);
            Assert.Equal("recipient@example.com", transport.LastMessage!.To);
            Assert.Contains("Password", transport.LastMessage.Subject);
            Assert.Contains("https://localhost/reset-password?token=123&email=recipient@example.com", transport.LastMessage.Body);
            Assert.True(transport.LastMessage.IsHtml);
            Assert.NotNull(transport.LastOptions);
            Assert.Equal("noreply@example.com", transport.LastOptions!.FromAddress);
            Assert.Equal("AIShopVerse", transport.LastOptions.FromDisplayName);
            Assert.Equal(587, transport.LastOptions.SmtpPort);
            Assert.True(transport.LastOptions.UseSsl);
        }

        [Fact]
        public async Task Enabled_ButHostMissing_DoesNotSend()
        {
            var service = CreateService(out var transport,
                ("Email:Enabled", "true"),
                ("Email:FromAddress", "noreply@example.com"));

            await service.SendPasswordResetEmailAsync("user@example.com", "https://localhost/reset?token=abc");

            Assert.Equal(0, transport.CallCount);
        }

        [Fact]
        public async Task TransportFailure_IsCaught_DoesNotPropagate()
        {
            var service = CreateService(out var transport,
                ("Email:Enabled", "true"),
                ("Email:SmtpHost", "smtp.example.com"),
                ("Email:FromAddress", "noreply@example.com"));
            transport.ThrowOnSend = new InvalidOperationException("SMTP refused connection");

            await service.SendPasswordResetEmailAsync("user@example.com", "https://localhost/reset?token=abc");

            Assert.Equal(1, transport.CallCount);
        }

        private static SmtpEmailService CreateService(out FakeSmtpTransport transport, params (string Key, string? Value)[] configValues)
        {
            var config = new TestConfiguration(CreateValues(configValues));
            transport = new FakeSmtpTransport();
            return new SmtpEmailService(config, transport, NullLogger<SmtpEmailService>.Instance);
        }

        private static Dictionary<string, string?> CreateValues(params (string Key, string? Value)[] values)
        {
            var dict = new Dictionary<string, string?>(StringComparer.OrdinalIgnoreCase);
            foreach (var (key, value) in values)
            {
                dict[key] = value;
            }
            return dict;
        }

        private sealed class FakeSmtpTransport : ISmtpTransport
        {
            public EmailMessage? LastMessage { get; private set; }
            public EmailOptions? LastOptions { get; private set; }
            public int CallCount { get; private set; }
            public Exception? ThrowOnSend { get; set; }

            public Task SendAsync(EmailMessage message, EmailOptions options, CancellationToken cancellationToken = default)
            {
                CallCount++;
                LastMessage = message;
                LastOptions = options;

                if (ThrowOnSend != null)
                {
                    var exception = ThrowOnSend;
                    ThrowOnSend = null;
                    throw exception;
                }

                return Task.CompletedTask;
            }
        }
    }
}
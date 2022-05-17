using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace Server.Engines.Chat
{
    public class Channel
    {
        public static void Initialize()
        {
            EventSink.Disconnected += EventSink_Disconnected;

            // TODO: Add a configuration framework to define static channels outside of code, for example as XML under the Data/ directory.
            AddStaticChannel("Help");
            AddStaticChannel("General");
            AddStaticChannel("Trade");
            AddStaticChannel("Looking For Group");
        }

        private static void EventSink_Disconnected(DisconnectedEventArgs e)
        {
            ChatUser.RemoveChatUser(e.Mobile);
        }

        public static void AddStaticChannel(string name)
        {
            AddChannel(name).AlwaysAvailable = true;
        }

        private readonly string m_Name;
        private bool m_AlwaysAvailable;
        private readonly List<ChatUser> m_Users;

        public Channel(string name)
        {
            m_Name = name;

            m_Users = new List<ChatUser>();
        }

        public string Name => m_Name;

        public IEnumerable<ChatUser> Users => new ReadOnlyCollection<ChatUser>(m_Users);

        public bool Contains(ChatUser user)
        {
            return m_Users.Contains(user);
        }

        public void AddUser(ChatUser user)
        {
            if (Contains(user))
            {
                user.SendMessage(46, m_Name); // You are already in the conference '%1'.
            }
            else
            {
                if (user.CurrentChannel != null)
                    user.CurrentChannel.RemoveUser(user); // Remove them from their current channel first

                ChatSystem.SendCommandTo(user.Mobile, ChatCommand.JoinedChannel, m_Name);

                SendCommand(ChatCommand.AddUserToChannel, ChatUser.GetColorCharacter() + user.Username);

                m_Users.Add(user);
                user.CurrentChannel = this;

                SendUsersTo(user);

                ChatLogging.LogJoin(Name, user.Username);
            }
        }

        public void RemoveUser(ChatUser user)
        {
            if (Contains(user))
            {
                m_Users.Remove(user);
                user.CurrentChannel = null;

                SendCommand(ChatCommand.RemoveUserFromChannel, user, user.Username);

                ChatSystem.SendCommandTo(user.Mobile, ChatCommand.LeaveChannel, $"{{{m_Name}}}");
                ChatSystem.SendCommandTo(user.Mobile, ChatCommand.LeftChannel, m_Name);

                ChatLogging.LogLeave(Name, user.Username);

                if (m_Users.Count == 0 && !m_AlwaysAvailable)
                    RemoveChannel(this);
            }
        }

        public bool AlwaysAvailable { get => m_AlwaysAvailable; set => m_AlwaysAvailable = value; }

        public void SendMessage(int number, ChatUser from, string param1, string param2)
        {
            for (var index = 0; index < m_Users.Count; index++)
            {
                ChatUser user = m_Users[index];

                if (user.CheckOnline())
                {
                    user.SendMessage(number, from.Mobile, param1, param2);
                }
            }
        }

        public void SendCommand(ChatCommand command, string param1 = null, string param2 = null)
        {
            SendCommand(command, null, param1, param2);
        }

        public void SendCommand(ChatCommand command, ChatUser initiator, string param1 = null, string param2 = null)
        {
            for (var index = 0; index < m_Users.ToArray().Length; index++)
            {
                ChatUser user = m_Users.ToArray()[index];

                if (user == initiator)
                {
                    continue;
                }

                if (user.CheckOnline())
                {
                    ChatSystem.SendCommandTo(user.Mobile, command, param1, param2);
                }
            }
        }

        public void SendUsersTo(ChatUser to)
        {
            for (var index = 0; index < m_Users.Count; index++)
            {
                ChatUser user = m_Users[index];

                ChatSystem.SendCommandTo(to.Mobile, ChatCommand.AddUserToChannel, ChatUser.GetColorCharacter() + user.Username, $"{{{m_Name}}}");
            }
        }

        private static readonly List<Channel> m_Channels = new List<Channel>();

        public static List<Channel> Channels => m_Channels;

        public static void SendChannelsTo(ChatUser user)
        {
            for (var index = 0; index < m_Channels.Count; index++)
            {
                Channel channel = m_Channels[index];

                ChatSystem.SendCommandTo(user.Mobile, ChatCommand.AddChannel, channel.Name, "0");
            }
        }

        public static Channel AddChannel(string name)
        {
            Channel channel = FindChannelByName(name);

            if (channel == null)
            {
                channel = new Channel(name);
                m_Channels.Add(channel);
            }

            ChatUser.GlobalSendCommand(ChatCommand.AddChannel, name, "0");

            ChatLogging.LogCreateChannel(name);

            return channel;
        }

        public static void RemoveChannel(string name)
        {
            RemoveChannel(FindChannelByName(name));
        }

        public static void RemoveChannel(Channel channel)
        {
            if (channel == null)
                return;

            if (m_Channels.Contains(channel) && channel.m_Users.Count == 0)
            {
                ChatUser.GlobalSendCommand(ChatCommand.RemoveChannel, channel.Name);

                m_Channels.Remove(channel);

                ChatLogging.LogRemoveChannel(channel.Name);
            }
        }

        public static Channel FindChannelByName(string name)
        {
            for (var index = 0; index < m_Channels.Count; index++)
            {
                var channel = m_Channels[index];

                if (channel.Name == name)
                {
                    return channel;
                }
            }

            return null;
        }

        public static Channel Default => FindChannelByName(ChatSystem.DefaultChannel);
    }
}

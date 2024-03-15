namespace UglyToad.PdfPig.Annotations
{
    using System;
    using System.Collections.Generic;
    using Tokens;

    /// <summary>
    /// Appearance stream (PDF Reference 8.4.4) that describes what an annotation looks like. Each stream is a Form XObject.
    /// The appearance stream is either stateless (in which case <see cref="IsStateless"/> is true)
    /// or stateful, in which case <see cref="IsStateless"/> is false and the states can be retrieved via <see cref="GetStates"/>.
    /// The states can then be used to retrieve the state-specific appearances using <see cref="Get"/>.
    /// </summary>
    public class AppearanceStream
    {
        private readonly IDictionary<string, StreamToken> appearanceStreamsByState;

        private readonly StreamToken statelessAppearanceStream;

        /// <summary>
        /// Indicates if this appearance stream is stateless, or whether you can get appearances by state.
        /// </summary>
        public bool IsStateless => statelessAppearanceStream != null;

        /// <summary>
        /// Get list of states. If this is a stateless appearance stream, an empty collection is returned.
        /// </summary>
        public ICollection<string> GetStates => appearanceStreamsByState != null ? appearanceStreamsByState.Keys : new string[0];

        /// <summary>
        /// Constructor for stateless appearance stream
        /// </summary>
        /// <param name="streamToken"></param>
        internal AppearanceStream(StreamToken streamToken)
        {
            statelessAppearanceStream = streamToken;
        }

        /// <summary>
        /// Constructor for stateful appearance stream
        /// </summary>
        /// <param name="appearanceStreamsByState"></param>
        internal AppearanceStream(IDictionary<string, StreamToken> appearanceStreamsByState)
        {
            this.appearanceStreamsByState = appearanceStreamsByState;
        }

        /// <summary>
        /// Get appearance stream for particular state
        /// </summary>
        /// <param name="state"></param>
        /// <returns></returns>
        /// <exception cref="Exception"></exception>
        /// <exception cref="ArgumentOutOfRangeException"></exception>
        public StreamToken Get(string state)
        {
            if (appearanceStreamsByState is null)
            {
                throw new Exception("Cannot get appearance by state when this is a stateless appearance stream");
            }
            if (!appearanceStreamsByState.ContainsKey(state))
            {
                throw new ArgumentOutOfRangeException(nameof(state), $"Appearance stream does not have state '{state}' (available states: {string.Join(",", appearanceStreamsByState.Keys)})");
            }
            return appearanceStreamsByState[state];
        }
    }
}

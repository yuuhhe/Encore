using System.Diagnostics.Contracts;
using System.Security.Cryptography;

namespace Trinity.Encore.Framework.Core.Cryptography.SRP
{
    /// <summary>
    /// Base class for client/server implementations of the Secure Remote Password
    /// (SRP) authentication protocol.
    /// </summary>
    [ContractClass(typeof(SRPBaseContracts))]
    public abstract class SRPBase
    {
        protected SRPBase(string username, BigInteger credentials, SRPParameters parameters)
        {
            Contract.Requires(!string.IsNullOrEmpty(username));
            Contract.Requires(credentials != null);
            Contract.Requires(parameters != null);

            Parameters = parameters;

            if (!parameters.CaseSensitive)
                username = username.ToUpper();

            Username = username;
            Credentials = credentials;
            SecretValue = parameters.RandomNumber(parameters.KeyLength);
            Validator = new SRPValidator(this);
        }

        [ContractInvariantMethod]
        private void Invariant()
        {
            Contract.Invariant(Parameters != null);
            Contract.Invariant(Validator != null);
            Contract.Invariant(SecretValue != null);
            Contract.Invariant(Username != null);
            Contract.Invariant(Credentials != null);
        }

        public BigInteger Hash(params HashDataBroker[] brokers)
        {
            Contract.Requires(brokers != null);
            Contract.Requires(brokers.Length > 0);
            Contract.Ensures(Contract.Result<BigInteger>() != null);
            Contract.Ensures(Contract.Result<BigInteger>().ByteLength == Parameters.HashLength);

            return Parameters.Hash.FinalizeHash(brokers);
        }

        private BigInteger _credentialsHash;

        private BigInteger _salt;

        public SRPParameters Parameters { get; private set; }

        public SRPValidator Validator { get; private set; }

        protected BigInteger PublicA { get; set; }

        protected BigInteger PublicB { get; set; }

        protected BigInteger RawSessionKey { get; set; }

        /// <summary>
        /// Represents a for the client and b for the server.
        /// </summary>
        public BigInteger SecretValue { get; private set; }

        /// <summary>
        /// Are we the server? This should be set before calculations happen.
        /// </summary>
        public abstract bool IsServer { get; }

        /// <summary>
        /// I in the specification. This should be set before calculations happen.
        /// </summary>
        public string Username { get; private set; }

        /// <summary>
        /// This is p in the specification, although not plain text.
        /// </summary>
        public BigInteger Credentials { get; private set; }

        /// <summary>
        /// This is A in the specification. A = g ^ a, generated by the client and sent to the server.
        /// </summary>
        public abstract BigInteger PublicEphemeralValueA { get; set; }

        /// <summary>
        /// This is B in the specification. B = kv + g ^ b, generated by the server and sent to the client.
        /// </summary>
        public abstract BigInteger PublicEphemeralValueB { get; set; }

        /// <summary>
        /// This is K in the specification. Note that this is different to k (multiplier).
        /// 
        /// This is the session key used for encryption later.
        /// </summary>
        public abstract BigInteger SessionKeyRaw { get; }

        /// <summary>
        /// This is s in the specification; salt for credentials hash. You can bind this to the user's
        /// account or use the automatically-generated random salt.
        /// </summary>
        public BigInteger Salt
        {
            get
            {
                Contract.Ensures(Contract.Result<BigInteger>() != null);

                if (_salt == null)
                {
                    if (!IsServer)
                        throw new CryptographicException("No client salt - should be set by the server.");

                    _salt = Parameters.RandomNumber(Parameters.KeyLength);
                }

                return _salt;
            }
            set { _salt = value; }
        }

        /// <summary>
        /// This is x in the specification; x = H(s, p).
        /// </summary>
        public BigInteger CredentialsHash
        {
            get { return _credentialsHash ?? (_credentialsHash = Hash(Salt, Credentials)); }
        }

        /// <summary>
        /// This is u in the specification. Generated by both server and client.
        /// </summary>
        public BigInteger ScramblingParameter
        {
            get
            {
                Contract.Ensures(Contract.Result<BigInteger>() != null);

                var u = Hash(PublicEphemeralValueA, PublicEphemeralValueB);

                if (!IsServer && u == 0)
                    throw new CryptographicException("The value of u cannot be 0.");

                return u;
            }
        }

        public BigInteger SessionKey
        {
            get
            {
                Contract.Ensures(Contract.Result<BigInteger>() != null);

                var hashSize = Parameters.HashLength;
                var keySize = Parameters.KeyLength;

                var data = SessionKeyRaw.GetBytes(keySize);
                var temp = new byte[keySize / 2];

                for (var i = 0; i < temp.Length; i++)
                    temp[i] = data[2 * i];

                var hash1 = Hash(temp).GetBytes(hashSize);

                for (var i = 0; i < temp.Length; i++)
                    temp[i] = data[2 * i + 1];

                var hashSize2 = Parameters.HashLength; // To shut the contract tools up.
                var hash2 = Hash(temp).GetBytes(hashSize2);
                var newData = new byte[hashSize * 2];

                for (var i = 0; i < newData.Length; i++)
                    newData[i] = i % 2 == 0 ? hash1[i / 2] : hash2[i / 2];

                return data;
            }
        }

        /// <summary>
        /// Generates a hash for an account's credentials (username:password).
        /// </summary>
        /// <param name="parameters">The parameters to use in calculations.</param>
        /// <param name="username">The username.</param>
        /// <param name="password">The password.</param>
        /// <param name="caseSensitive">Whether or not username/password should
        /// be case-sensitive.</param>
        public static byte[] GenerateCredentialsHash(SRPParameters parameters, string username, string password,
            bool caseSensitive = true)
        {
            Contract.Requires(!string.IsNullOrEmpty(username));
            Contract.Requires(!string.IsNullOrEmpty(password));
            Contract.Ensures(Contract.Result<byte[]>() != null);

            // Just use 6a here; it makes no difference during account creation.
            var user = caseSensitive ? username : username.ToUpper();
            var pass = caseSensitive ? password : password.ToUpper();
            var str = string.Format("{0}:{1}", user, pass);

            return parameters.Hash.ComputeHash(SRPParameters.StringEncoding.GetBytes(str));
        }
    }

    [ContractClassFor(typeof(SRPBase))]
    public abstract class SRPBaseContracts : SRPBase
    {
        protected SRPBaseContracts(string username, BigInteger credentials, SRPParameters parameters)
            : base(username, credentials, parameters)
        {
        }

        public override BigInteger PublicEphemeralValueA
        {
            get
            {
                Contract.Ensures(Contract.Result<BigInteger>() != null);

                return null;
            }
            set { Contract.Requires(value != null); }
        }

        public override BigInteger PublicEphemeralValueB
        {
            get
            {
                Contract.Ensures(Contract.Result<BigInteger>() != null);

                return null;
            }
            set { Contract.Requires(value != null); }
        }

        public override BigInteger SessionKeyRaw
        {
            get
            {
                Contract.Ensures(Contract.Result<BigInteger>() != null);
                Contract.Ensures(Contract.Result<BigInteger>().ByteLength == Parameters.KeyLength);

                return null;
            }
        }
    }
}

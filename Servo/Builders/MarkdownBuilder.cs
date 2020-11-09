namespace Servo.Builders
{
    internal class MarkdownBuilder
    {
        private string _text;

        public static implicit operator string(MarkdownBuilder builder) => builder._text;

        public MarkdownBuilder()
        {
            _text = "";
        }

        public MarkdownBuilder(object obj) : this(obj.ToString())
        {

        }

        public MarkdownBuilder(string text)
        {
            _text = text;
        }

        public MarkdownBuilder Append(MarkdownBuilder builder, bool spacing = false)
        {
            _text = string.Join(spacing ? " " : "", _text, builder);
            return this;
        }

        public MarkdownBuilder Bold()
        {
            _text = $"**{_text}**";
            return this;
        }

        public MarkdownBuilder Italic()
        {
            _text = $"*{_text}*";
            return this;
        }

        public MarkdownBuilder Underline()
        {
            _text = $"__{_text}__";
            return this;
        }

        public MarkdownBuilder Strikethrough()
        {
            _text = $"~~{_text}~~";
            return this;
        }

        public MarkdownBuilder SingleCodeBlock()
        {
            _text = $"`{_text}`";
            return this;
        }

        public MarkdownBuilder MultiCodeBlock(string language = "")
        {
            _text = $"```{language}\n" +
                    $"{_text}\n" +
                    $"```";
            return this;
        }

        public MarkdownBuilder SingleBlockQuote()
        {
            _text = $"> {_text}";
            return this;
        }

        public MarkdownBuilder MultipleBlockQuote()
        {
            _text = $">>> {_text}";
            return this;
        }

        public MarkdownBuilder Spoiler()
        {
            _text = $"||{_text}||";
            return this;
        }

        public override string ToString()
        {
            return _text;
        }
    }
}

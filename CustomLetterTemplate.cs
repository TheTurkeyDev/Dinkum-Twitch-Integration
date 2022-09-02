namespace DinkumTwitchIntegration
{
    public class CustomLetterTemplate : LetterTemplate
    {
        public CustomLetterTemplate()
        {
            signOff = "Love, Chat not ";
            gift = null;
            giftFromTable = null;
            stackOfGift = 0;
        }

        public CustomLetterTemplate(string text)
        {
            letterText = text;
            signOff = "Love, Chat not ";
            gift = null;
            giftFromTable = null;
            stackOfGift = 0;
        }
    }
}

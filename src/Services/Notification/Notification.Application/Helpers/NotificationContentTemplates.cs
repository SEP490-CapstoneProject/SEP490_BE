namespace Notification.Application.Helpers;

public static class NotificationContentTemplates
{
    public static class PostFavorite
    {
        public static string SingleFavorite(string actorName) 
            => $"{actorName} đã thích bài viết của bạn";
            
        public static string MultipleFavorites(int count) 
            => $"{count} người đã thích bài viết của bạn";
    }
    
    public static class PostComment 
    {
        public static string NewComment(string actorName)
            => $"{actorName} đã bình luận bài viết của bạn";
    }
    
    public static class PostReply
    {
        public static string NewReply(string actorName, string originalCommenterName)
            => $"{actorName} đã trả lời bình luận của {originalCommenterName}";
    }
}
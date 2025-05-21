
using System.Text.Json.Serialization;

namespace Calibre_net.Shared.Contracts;

public class Permission
{
    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    [JsonPropertyName("description")]
    public string Description { get; set; } = string.Empty;
}

public static class PermissionType
{
    public const string ADMIN = "Admin";
    public const string ADMIN_USER = "Admin.User";
    public const string BOOK_VIEW = "Book.View";
    // public const string BOOK_VIEW = "Admin";
    public const string BOOK_BOOKMARK = "Book.Bookmark";
    public const string CONFIGURATION_EDIT = "Configuration.Edit";
    public const string CONFIGURATION_VIEWALL = "Configuration.ViewAll";
}
public static class PermissionStore
{
    public static List<Permission> GetPermissions()
    {
        return new List<Permission>()
        {
            new()
            {
                Name = PermissionType.ADMIN,
                Description = "Can access and edit Admin screens."
            },
            new()
            {
                Name = "Edit",
                Description = "Can Edit info."
            },
            new()
            {
                Name = "Delete",
                Description = "Can Delete content."
            },
            new()
            {
                Name = "Download.Allow",
                Description = "Can Download content."
            },
            new()
            {
                Name = "Viewer.Allow",
                Description = "Can use in browser content viewer."
            },
            new()
            {
                Name = "PasswordChange.Allow",
                Description = "Can change passwords of users."
            },
            new()
            {
                Name = "PublicShelf.Edit",
                Description = "Can edit public shelves."
            },
            new()
            {
                Name = "Hot.Show",
                Description = ""
            },
            new()
            {
                Name = "Downloaded.Show",
                Description = ""
            },
            new()
            {
                Name = "TopRated.Show",
                Description = ""
            },
            new()
            {
                Name = "ReadUnread.Show",
                Description = ""
            },
            new()
            {
                Name = "Random.Show",
                Description = ""
            },
            new()
            {
                Name = "Category.Show",
                Description = ""
            },
            new()
            {
                Name = "Series.Show",
                Description = ""
            },
            new()
            {
                Name = "Author.Show",
                Description = ""
            },
            new()
            {
                Name = "Publisher.Show",
                Description = ""
            },
            new()
            {
                Name = "Language.Show",
                Description = ""
            },
            new()
            {
                Name = "Ratings.Show",
                Description = ""
            },
            new()
            {
                Name = "FileFormat.Show",
                Description = ""
            },
            new()
            {
                Name = "Archived.Show",
                Description = ""
            },
            new()
            {
                Name = "List.Show",
                Description = ""
            },
            new()
            {
                Name = "RandomDetailView.Show",
                Description = ""
            },
            // new()
            // {
            //     Name = "User.Add",
            //     Description = "Add a User"
            // },
            // new()
            // {
            //     Name = "User.Edit",
            //     Description = "Edit a User"
            // },
            // new()
            // {
            //     Name = "User.Delete",
            //     Description = "Delete a User"
            // },
            new()
            {
                Name = PermissionType.ADMIN_USER,
                Description = "Manage users (List/Add/Edit/Delete)"
            },
            new()
            {
                Name = "User.EditSelf",
                Description = "Edit yourself"
            },
            new()
            {
                Name = "User.DeleteSelf",
                Description = "Delete yourself"
            },
            new()
            {
                Name = PermissionType.BOOK_VIEW,
                Description = "List, View Details, Read, Download Book"
            },
            new()
            {
                Name = PermissionType.BOOK_BOOKMARK,
                Description = "Delete yourself"
            },
            new()
            {
                Name = PermissionType.CONFIGURATION_VIEWALL,
                Description = "Get All Confirguration Settings"
            },
            new()
            {
                Name = PermissionType.CONFIGURATION_EDIT,
                Description = "Edit Configuration"
            }



        };
    }

    public static bool HasPermission(this UserModel user, string permission)
    {
        return user.Permissions.Contains(permission);
    }

    public static bool HasPermission(this UserModelExtended user, string permission)
    {
        // return user.Permissions.Contains(permission);
        if (user.PermissionsDictionary == null)
            return false;
        return user.PermissionsDictionary.ContainsKey(permission) ? user.PermissionsDictionary[permission] == "on" : false;
    }

}
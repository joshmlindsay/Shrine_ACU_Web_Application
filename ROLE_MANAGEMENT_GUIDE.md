# Role Management Guide

## Purpose

Roles are assigned per application. This keeps Attendance management separate from judging and car-show management, so users only see the navigation items and pages required for their work.

## Assigning a Role

1. Sign in with an account that has access to **Manage Users**.
2. Open **Manage Users** from the navigation menu.
3. Find the user using the column filters or virtualized list.
4. Select **Manage Roles**.
5. Select the application, such as **Attendance**.
6. Select the role and permissions.
7. Select **Save Role Access**.

The user must refresh their session or sign in again to receive the updated navigation and page access. If an administrator updates their own role, the page refreshes the current session automatically.

## Recommended Assignments

| Need | Application | Role | Permissions |
| --- | --- | --- | --- |
| Manage attendance events, attendees, and records | Attendance | Manager | Read, Write, Manage |
| Record attendance without management capability | Attendance | Participant | Read, Write |
| Score judging events | Scoring or Car Shows | Judge | Read, Write |
| Manage scoring events and entries | Scoring or Car Shows | Manager | Read, Write, Manage |
| Full administration for an application | Relevant application | Admin | Read, Write, Manage |

## Visibility Rules

- **Attendance Manager**: sees **Manage Attendance**, but does not receive scoring-event administration simply from that role.
- **Judge**: sees **Scoring** for the assigned scoring/car-show application, but does not see **Manage Attendance** unless separately assigned an Attendance Manager or Admin role.
- **Manager** and **Admin**: can manage only the application where the role is assigned.
- **Participant**: receives normal read/write access for the assigned application without management controls.

## Managing Attendance Access

To give someone attendance-management access:

1. Open **Manage Users**.
2. Select **Manage Roles** for that user.
3. Choose the **Attendance** application.
4. Select **Manager**.
5. Enable **Read**, **Write**, and **Manage**.
6. Save the role access.

Do not assign a scoring or car-show role unless the user also needs those capabilities.

## Notes

- Removing a role or clearing its permissions removes the associated navigation after the user refreshes their session.
- Application names determine which UI areas are shown. The Attendance application should include `Attendance` in its name. Scoring applications should include `Scoring`, `Judge`, `Car Show`, or `CarShow` in their name.
- The API remains the authority for access control. UI visibility improves usability but does not replace API authorization.

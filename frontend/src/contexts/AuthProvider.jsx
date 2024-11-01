import axios from "axios";
import { memo, useEffect, useMemo, useState } from "react";
import { AuthContext } from "./AuthContext";
import { RolePermissions } from "../constants/RolePermissions"; // Import role permissions

// Create the authentication provider
// eslint-disable-next-line react/display-name
export const AuthProvider = memo(({ children }) => {
    // State to hold the token
    const [token, setToken_] = useState(localStorage.getItem("token"));
    const [user, setUser_] = useState(null); // State for user data

    // Function to update the authentication token
    const setToken = (newToken) => {
        if (newToken) {
            setToken_(newToken);
            localStorage.setItem('token', newToken);
        } else {
            setToken_(null);
            localStorage.removeItem('token');
        }
    };

    // Function to update the user state and assign permissions
    const setUser = (newUser) => {
        if (newUser) {
            // Assign permissions based on role or specific permissions
            const permissions = newUser.role
                ? RolePermissions[newUser.role.name] || [] // Assign role-based permissions
                : newUser.permissions || []; // Assign directly if no role
            setUser_({ ...newUser, permissions });
        } else {
            setUser_(null); // Clear user when logged out
        }
    };

    // Update Axios authorization headers whenever the token changes
    useEffect(() => {
        if (token) {
            axios.defaults.headers.common["Authorization"] = `Bearer ${token}`;
        } else {
            delete axios.defaults.headers.common["Authorization"];
            setUser(null); // Clear user when no token is available
        }

        // Simulate setting the user based on the current token
        if (token) {
            // Example user, replace with actual user fetch logic
          // const exampleUser = { name: "santiago", role: { name: 'oficial' }, userId: "222" };
           // setUser(exampleUser);
            //setUser(null);
           const exampleUser={
                userId: "3333333",
                name: "Fabiana Madrigal Flores",
                idNumber: "3333333",
                email: "fabiana.madrigal.f@gmail.com",
                password: "Fabi1301",
                phoneNumber: "22222222",
                role: {name: "oficial"},
                profilePicture: ""
            }
            setUser(exampleUser);
        }

    }, [token]);

    // If no user is logged in, assign default guest permissions
    const userWithPermissions = user
        && user;

    // Memoize the context value to optimize performance
    const contextValue = useMemo(
        () => ({
            token,
            setToken,
            user: userWithPermissions, // Provide the user with permissions
            setUser
        }),
        [token, userWithPermissions]
    );

    return (
        <AuthContext.Provider value={contextValue}>
            {children}
        </AuthContext.Provider>
    );
});

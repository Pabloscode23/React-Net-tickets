import { useEffect, useState } from "react";
import { UserClaims } from "../components/UserClaims";
import { faMagnifyingGlass } from '@fortawesome/free-solid-svg-icons';
import { FontAwesomeIcon } from '@fortawesome/react-fontawesome';
import axios from 'axios';
import { useAuth } from "../../../hooks";
import { API_URL } from "../../../constants/Api";
import { TicketsInfo } from "../../../constants/TicketsInfo";

export const OfficerClaimsPage = () => {
    const { user } = useAuth();
    const [claims, setClaims] = useState([]);
    const [searchTerm, setSearchTerm] = useState("");


    useEffect(() => {
        const fetchClaims = async () => {
            try {
                const response = await axios.get(`${API_URL}/TicketDTO`);

                const claims = response.data
                    .filter(claim => claim.officerId === user.userId && claim.status !== "Pendiente") // Use strict equality
                    .map(claim => {
                        console.log("Ticket Description:", claim.description);

                        const amount = TicketsInfo[claim.description] || 0;

                        return {
                            ...claim,
                            status: claim.status,
                            amount
                        };
                    });
                setClaims(claims);                // Set the filtered claims in state
            } catch (error) {
                console.error("Error fetching claims:", error);
            }
        };

        fetchClaims();
    }, [user]); // Fetch claims when userId changes

    const filteredClaims = claims.filter((ticket) =>
        ticket.id.toString().includes(searchTerm) ||
        ticket.date.includes(searchTerm) ||
        ticket.reason.toLowerCase().includes(searchTerm.toLowerCase()) ||
        ticket.amount.includes(searchTerm) ||
        ticket.status.toLowerCase().includes(searchTerm.toLowerCase())
    );

    return (
        <div className="container__tickets">
            <h1 className="main__ticket-title">Reclamos</h1>
            <h2 className="main__ticket-subtitle">Aquí encuentra las multas reclamadas que su persona ha hecho</h2>
            <div className="search__container">
                <FontAwesomeIcon icon={faMagnifyingGlass} className="search__icon" />
                <input
                    type="text"
                    placeholder="Buscar multa"
                    className="search__ticket"
                    value={searchTerm} // Bind input value to searchTerm
                    onChange={(e) => setSearchTerm(e.target.value)} // Update searchTerm on input change
                />
            </div>
            <table className="ticket-table">
                <thead>
                    <tr className='table__head'>
                        <th>ID multa</th>
                        <th>Fecha</th>
                        <th>Razón de la multa</th>
                        <th>Monto de la multa</th>
                        <th>Estado</th>
                    </tr>
                </thead>
                <tbody className='table__children'>
                    {filteredClaims.map((ticket) => (
                        <UserClaims key={ticket.id}
                            id={ticket.id}
                            date={ticket.date}
                            reason={ticket.description}
                            amount={ticket.amount}
                            status={ticket.status}
                        />
                    ))}
                </tbody>
            </table>
        </div>
    );
};
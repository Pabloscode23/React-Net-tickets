import { Link } from 'react-router-dom'

export const NotFound = () => {
    return (
        <>
            <h1>404 No encontrado</h1>
            <p>La página que estás buscando no existe</p>
            <Link to="/">Volver al inicio</Link>
        </>
    )
}
import React from 'react';
import StyledLink from "./styled-link";
import { useAuth } from '@/hooks/use-dynamic-auth';

interface Props {

}

const LoginButton: React.FC<Props> = (props) => {
   const { user } = useAuth();

   return (
      <div>
         {user
            ? <StyledLink href="/projects" variant="default" size="sm"
            >Dashboard</StyledLink>
            : <StyledLink href={`/login?redirect=${location.origin}/projects`} variant="default" size="sm"
            >Login</StyledLink>}
      </div>
   );
}

export default LoginButton;
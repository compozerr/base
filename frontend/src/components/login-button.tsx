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
            ? <StyledLink href="/dashboard" variant="default" size="sm"
            >Dashboard</StyledLink>
            : <StyledLink href={`/login?redirect=${location.origin}/dashboard`} variant="default" size="sm"
            >Login</StyledLink>}
      </div>
   );
}

export default LoginButton;
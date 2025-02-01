import React from 'react';
import StyledLink from "./styled-link";

interface Props {

}

const LoginButton: React.FC<Props> = (props) => {
   return (
      <div>
         <StyledLink href="/login" variant="default" size="sm"
         >Login</StyledLink>
      </div>
   );
}

export default LoginButton;